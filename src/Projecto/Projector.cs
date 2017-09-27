/*
 * Copyright 2017 Wouter Huysentruit
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Projecto.DependencyInjection;

namespace Projecto
{
    /// <summary>
    /// Projector class for projecting messages to registered projections.
    /// </summary>
    /// <typeparam name="TProjectionKey">The type of the key that uniquely identifies a projection.</typeparam>
    /// <typeparam name="TMessageEnvelope">The type of the message envelope used to pass the message including custom information to the handler.</typeparam>
    /// <typeparam name="TNextSequenceNumberRepository">The type of the <see cref="INextSequenceNumberRepository{TProjectionKey}"/> implementation to use.</typeparam>
    public class Projector<TProjectionKey, TMessageEnvelope, TNextSequenceNumberRepository>
        where TProjectionKey : IEquatable<TProjectionKey>
        where TMessageEnvelope : MessageEnvelope
        where TNextSequenceNumberRepository : INextSequenceNumberRepository<TProjectionKey>
    {
        private readonly HashSet<IProjection<TProjectionKey, TMessageEnvelope>> _projections;
        private readonly IDependencyLifetimeScopeFactory _dependencyLifetimeScopeFactory;
        private Dictionary<TProjectionKey, int> _nextSequenceNumberCache = null;
        private int? _nextSequenceNumber = null;
        private readonly object _syncRoot = new object();

        /// <summary>
        /// Internal constructor, used by <see cref="ProjectorBuilder{TProjectionKey, TMessageEnvelope}"/>.
        /// </summary>
        /// <param name="projections">The registered projections.</param>
        /// <param name="dependencyLifetimeScopeFactory">The dependency lifetime scope factory.</param>
        internal Projector(HashSet<IProjection<TProjectionKey, TMessageEnvelope>> projections, IDependencyLifetimeScopeFactory dependencyLifetimeScopeFactory)
        {
            if (projections == null) throw new ArgumentNullException(nameof(projections));
            if (!projections.Any()) throw new ArgumentException("No projections registered", nameof(projections));
            if (dependencyLifetimeScopeFactory == null) throw new ArgumentNullException(nameof(dependencyLifetimeScopeFactory));
            GuaranteeKeyUniqueness(projections);
            _projections = projections;
            _dependencyLifetimeScopeFactory = dependencyLifetimeScopeFactory;
        }

        /// <summary>
        /// Returns the dependency lifetime scope factory for internal usage and unit-testing.
        /// </summary>
        internal IDependencyLifetimeScopeFactory DependencyLifetimeScopeFactory => _dependencyLifetimeScopeFactory;

        /// <summary>
        /// Returns the Projections for internal usage and unit-testing.
        /// </summary>
        internal IProjection<TProjectionKey, TMessageEnvelope>[] Projections => _projections.ToArray();

        /// <summary>
        /// Gets the next event sequence number needed by the most out-dated registered projection.
        /// </summary>
        /// <returns>The next event sequence number.</returns>
        public async Task<int> GetNextSequenceNumber()
        {
            if (!_nextSequenceNumber.HasValue)
            {
                using (var scope = _dependencyLifetimeScopeFactory.BeginLifetimeScope())
                {
                    var nextSequenceNumberRepository = ResolveNextSequenceNumberRepository(scope);
                    await CacheNextSequenceNumbers(nextSequenceNumberRepository).ConfigureAwait(false);
                }
            }

            return _nextSequenceNumber ?? 1;
        }

        /// <summary>
        /// Project a message to all registered projections.
        /// </summary>
        /// <param name="messageEnvelope">The message envelope.</param>
        /// <returns>A <see cref="Task"/> for async execution.</returns>
        public Task Project(TMessageEnvelope messageEnvelope) => Project(messageEnvelope, CancellationToken.None);

        /// <summary>
        /// Project a message to all registered projections with cancellation support.
        /// </summary>
        /// <param name="messageEnvelope">The message envelope.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A <see cref="Task"/> for async execution.</returns>
        public Task Project(TMessageEnvelope messageEnvelope, CancellationToken cancellationToken) => Project(new[] { messageEnvelope }, cancellationToken);

        /// <summary>
        /// Projects multiple messages to all registered projections.
        /// </summary>
        /// <param name="messageEnvelopes">The message envelopes.</param>
        /// <returns>A <see cref="Task"/> for async execution.</returns>
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public Task Project(TMessageEnvelope[] messageEnvelopes) => Project(messageEnvelopes, CancellationToken.None);

        /// <summary>
        /// Projects multiple messages to all registered projections with cancellation support.
        /// </summary>
        /// <param name="messageEnvelopes">The message envelopes.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A <see cref="Task"/> for async execution.</returns>
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public async Task Project(TMessageEnvelope[] messageEnvelopes, CancellationToken cancellationToken)
        {
            if (!messageEnvelopes.Any()) return;

            using (var scope = _dependencyLifetimeScopeFactory.BeginLifetimeScope())
            {
                var nextSequenceNumberRepository = ResolveNextSequenceNumberRepository(scope);
                await CacheNextSequenceNumbers(nextSequenceNumberRepository).ConfigureAwait(false);

                try
                {
                    foreach (var messageEnvelope in messageEnvelopes)
                    {
                        if (messageEnvelope.SequenceNumber != _nextSequenceNumber)
                            throw new ArgumentOutOfRangeException(nameof(messageEnvelope.SequenceNumber),
                                $"Message {messageEnvelope.Message.GetType()} has invalid sequence number {messageEnvelope.SequenceNumber} instead of {_nextSequenceNumber}");

                        foreach (var projection in _projections)
                        {
                            if (_nextSequenceNumberCache[projection.Key] != messageEnvelope.SequenceNumber) continue;

                            await projection.Handle(() => scope.Resolve(projection.ConnectionType), messageEnvelope, cancellationToken).ConfigureAwait(false);
                            if (cancellationToken.IsCancellationRequested) return;
                            _nextSequenceNumberCache[projection.Key] = _nextSequenceNumberCache[projection.Key] + 1;
                        }

                        _nextSequenceNumber++;
                    }
                }
                finally
                {
                    await StoreNextSequenceNumbers(nextSequenceNumberRepository).ConfigureAwait(false);
                }
            }
        }

        private async Task CacheNextSequenceNumbers(TNextSequenceNumberRepository nextSequenceNumberRepository)
        {
            if (_nextSequenceNumber != null) return;

            var projectionKeys = new HashSet<TProjectionKey>(_projections.Select(x => x.Key));
            var sequenceNumbers = await nextSequenceNumberRepository.Fetch(projectionKeys).ConfigureAwait(false);
            var dictionary = projectionKeys.ToDictionary(x => x, x => sequenceNumbers.ContainsKey(x) ? sequenceNumbers[x] : 1);

            lock (_syncRoot)
            {
                if (_nextSequenceNumber != null) return;
                _nextSequenceNumberCache = dictionary;
                _nextSequenceNumber = _nextSequenceNumberCache.Values.Min();
            }
        }

        private Task StoreNextSequenceNumbers(TNextSequenceNumberRepository nextSequenceNumberRepository)
            => nextSequenceNumberRepository.Store(_nextSequenceNumberCache);

        private TNextSequenceNumberRepository ResolveNextSequenceNumberRepository(IDependencyLifetimeScope scope)
        {
            var nextSequenceNumberRepository = (TNextSequenceNumberRepository)scope.Resolve(typeof(TNextSequenceNumberRepository));
            if (nextSequenceNumberRepository == null) throw new InvalidOperationException($"Can't resolve {typeof(TNextSequenceNumberRepository).Name} from the dependency lifetime scope");
            return nextSequenceNumberRepository;
        }

        private static void GuaranteeKeyUniqueness(ISet<IProjection<TProjectionKey, TMessageEnvelope>> projections)
        {
            var duplicateKeys = projections
                .GroupBy(x => x.Key, (key, group) => new { Key = key, Count = group.Count() })
                .Where(x => x.Count > 1)
                .Select(x => x.Key.ToString())
                .ToArray();

            if (!duplicateKeys.Any()) return;

            throw new InvalidOperationException($"One or more projections use the same key ({string.Join(", ", duplicateKeys)})");
        }
    }
}
