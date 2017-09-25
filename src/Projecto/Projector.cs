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
    /// <typeparam name="TMessageEnvelope">The type of the message envelope used to pass the message including custom information to the handler.</typeparam>
    public class Projector<TMessageEnvelope>
        where TMessageEnvelope : MessageEnvelope
    {
        private readonly HashSet<IProjection<TMessageEnvelope>> _projections;
        private readonly IConnectionLifetimeScopeFactory _connectionLifetimeScopeFactory;
        private int? _nextSequenceNumber;

        /// <summary>
        /// Internal constructor, used by <see cref="ProjectorBuilder{TMessageEnvelope}"/>.
        /// </summary>
        /// <param name="projections">The registered projections.</param>
        /// <param name="connectionLifetimeScopeFactory">The connection lifetime scope factory.</param>
        internal Projector(HashSet<IProjection<TMessageEnvelope>> projections, IConnectionLifetimeScopeFactory connectionLifetimeScopeFactory)
        {
            if (projections == null) throw new ArgumentNullException(nameof(projections));
            if (!projections.Any()) throw new ArgumentException("No projections registered", nameof(projections));
            if (connectionLifetimeScopeFactory == null) throw new ArgumentNullException(nameof(connectionLifetimeScopeFactory));
            _projections = projections;
            _connectionLifetimeScopeFactory = connectionLifetimeScopeFactory;
        }

        /// <summary>
        /// Returns the connection lifetime scope factory for internal usage and unit-testing.
        /// </summary>
        internal IConnectionLifetimeScopeFactory ConnectionLifetimeScopeFactory => _connectionLifetimeScopeFactory;

        /// <summary>
        /// Returns the Projections for internal usage and unit-testing.
        /// </summary>
        internal IProjection<TMessageEnvelope>[] Projections => _projections.ToArray();

        /// <summary>
        /// Gets the next event sequence number needed by the most out-dated registered projection.
        /// </summary>
        public int GetNextSequenceNumber() => GetNextSequenceNumber(null);

        private int GetNextSequenceNumber(IConnectionLifetimeScope scope)
        {
            if (_nextSequenceNumber.HasValue) return _nextSequenceNumber.Value;

            var owned = scope == null;
            scope = scope ?? _connectionLifetimeScopeFactory.BeginLifetimeScope();

            _nextSequenceNumber = _projections
                .Select(projection => projection.GetNextSequenceNumber(scope))
                .Min();

            if (owned) scope.Dispose();

            return _nextSequenceNumber.Value;
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
            using (var scope = _connectionLifetimeScopeFactory.BeginLifetimeScope())
            {
                if (!_nextSequenceNumber.HasValue) GetNextSequenceNumber(scope);

                foreach (var messageEnvelope in messageEnvelopes)
                {
                    if (messageEnvelope.SequenceNumber != _nextSequenceNumber)
                        throw new ArgumentOutOfRangeException(nameof(messageEnvelope.SequenceNumber), 
                            $"Message {messageEnvelope.Message.GetType()} has invalid sequence number {messageEnvelope.SequenceNumber} instead of {_nextSequenceNumber}");

                    foreach (var projection in _projections)
                    {
                        if (projection.GetNextSequenceNumber(scope) != messageEnvelope.SequenceNumber) continue;

                        await projection.Handle(scope, messageEnvelope, cancellationToken).ConfigureAwait(false);
                        if (cancellationToken.IsCancellationRequested) return;
                        if (projection.GetNextSequenceNumber(scope) != messageEnvelope.SequenceNumber + 1)
                            throw new InvalidOperationException(
                                $"Projection {projection.GetType()} did not increment NextSequence ({messageEnvelope.SequenceNumber}) after processing message {messageEnvelope.Message.GetType()}");
                    }

                    _nextSequenceNumber++;
                }
            }
        }
    }
}
