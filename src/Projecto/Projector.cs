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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Projecto.Infrastructure;

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
        private readonly ProjectScopeFactory<TMessageEnvelope> _projectScopeFactory;
        private int? _nextSequenceNumber;

        /// <summary>
        /// Internal constructor, used by <see cref="ProjectorBuilder{TMessageEnvelope}"/>.
        /// </summary>
        /// <param name="projections">The registered projections.</param>
        /// <param name="projectScopeFactory">The project scope factory.</param>
        internal Projector(HashSet<IProjection<TMessageEnvelope>> projections, ProjectScopeFactory<TMessageEnvelope> projectScopeFactory)
        {
            if (projections == null) throw new ArgumentNullException(nameof(projections));
            if (!projections.Any()) throw new ArgumentException("No projections registered", nameof(projections));
            if (projectScopeFactory == null) throw new ArgumentNullException(nameof(projectScopeFactory));
            _projections = projections;
            _projectScopeFactory = projectScopeFactory;
        }

        /// <summary>
        /// Returns the project scope factory for internal usage and unit-testing.
        /// </summary>
        internal ProjectScopeFactory<TMessageEnvelope> ProjectScopeFactory => _projectScopeFactory;

        /// <summary>
        /// Returns the Projections for internal usage and unit-testing.
        /// </summary>
        internal IProjection<TMessageEnvelope>[] Projections => _projections.ToArray();

        /// <summary>
        /// Gets the next event sequence number needed by the most out-dated registered projection.
        /// </summary>
        public int NextSequenceNumber => _nextSequenceNumber ?? (int)(_nextSequenceNumber = _projections.Select(x => x.NextSequenceNumber).Min());

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
        public Task Project(IEnumerable<TMessageEnvelope> messageEnvelopes) => Project(messageEnvelopes, CancellationToken.None);

        /// <summary>
        /// Projects multiple messages to all registered projections with cancellation support.
        /// </summary>
        /// <param name="messageEnvelopes">The message envelopes.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A <see cref="Task"/> for async execution.</returns>
        public async Task Project(IEnumerable<TMessageEnvelope> messageEnvelopes, CancellationToken cancellationToken)
        {
            using (var scope = _projectScopeFactory(messageEnvelopes))
            {
                foreach (var messageEnvelope in messageEnvelopes)
                {
                    if (messageEnvelope.SequenceNumber != NextSequenceNumber)
                        throw new ArgumentOutOfRangeException(nameof(messageEnvelope.SequenceNumber), 
                            $"Message {messageEnvelope.Message.GetType()} has invalid sequence number {messageEnvelope.SequenceNumber} instead of {NextSequenceNumber}");

                    foreach (var projection in _projections.Where(x => x.NextSequenceNumber == messageEnvelope.SequenceNumber))
                    {
                        await projection.Handle(type => scope.InternalResolveConnection(type), messageEnvelope, cancellationToken).ConfigureAwait(false);
                        if (cancellationToken.IsCancellationRequested) return;
                        if (projection.NextSequenceNumber != messageEnvelope.SequenceNumber + 1)
                            throw new InvalidOperationException(
                                $"Projection {projection.GetType()} did not increment NextSequence ({messageEnvelope.SequenceNumber}) after processing message {messageEnvelope.Message.GetType()}");
                    }

                    _nextSequenceNumber++;
                }

                await scope.BeforeDispose().ConfigureAwait(false);
            }
        }
    }
}
