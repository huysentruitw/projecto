﻿/*
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
    /// <typeparam name="TProjectContext">The type of the project context (used to pass custom information to the handler).</typeparam>
    public class Projector<TProjectContext>
    {
        private readonly HashSet<IProjection<TProjectContext>> _projections;
        private readonly ProjectScopeFactory<TProjectContext> _projectScopeFactory;
        private int? _nextSequenceNumber;

        /// <summary>
        /// Internal constructor, used by <see cref="ProjectorBuilder{TProjectContext}"/>.
        /// </summary>
        /// <param name="projections">The registered projections.</param>
        /// <param name="projectScopeFactory">The project scope factory.</param>
        internal Projector(HashSet<IProjection<TProjectContext>> projections, ProjectScopeFactory<TProjectContext> projectScopeFactory)
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
        internal ProjectScopeFactory<TProjectContext> ProjectScopeFactory => _projectScopeFactory;

        /// <summary>
        /// Returns the Projections for internal usage and unit-testing.
        /// </summary>
        internal IProjection<TProjectContext>[] Projections => _projections.ToArray();

        /// <summary>
        /// Gets the next event sequence number needed by the most out-dated registered projection.
        /// </summary>
        public int NextSequenceNumber => _nextSequenceNumber ?? (int)(_nextSequenceNumber = _projections.Select(x => x.NextSequenceNumber).Min());

        /// <summary>
        /// Project a message to all registered projections.
        /// </summary>
        /// <param name="sequenceNumber">The sequence number of the message.</param>
        /// <param name="context">The project context (used to pass custom information to the handler).</param>
        /// <param name="message">The message.</param>
        /// <returns>A <see cref="Task"/> for async execution.</returns>
        public Task Project(int sequenceNumber, TProjectContext context, object message) => Project(sequenceNumber, context, message, CancellationToken.None);

        /// <summary>
        /// Project a message to all registered projections with cancellation support.
        /// </summary>
        /// <param name="sequenceNumber">The sequence number of the message.</param>
        /// <param name="context">The project context (used to pass custom information to the handler).</param>
        /// <param name="message">The message.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A <see cref="Task"/> for async execution.</returns>
        public async Task Project(int sequenceNumber, TProjectContext context, object message, CancellationToken cancellationToken)
        {
            if (sequenceNumber != NextSequenceNumber) throw new ArgumentOutOfRangeException(nameof(sequenceNumber), sequenceNumber, $"Expecting sequence {NextSequenceNumber}");

            using (var scope = _projectScopeFactory(context, message))
            {
                foreach (var projection in _projections.Where(x => x.NextSequenceNumber == sequenceNumber))
                {
                    await projection.Handle(type => scope.ResolveConnection(type), context, message, cancellationToken).ConfigureAwait(false);
                    if (cancellationToken.IsCancellationRequested) return;
                    if (projection.NextSequenceNumber != sequenceNumber + 1)
                        throw new InvalidOperationException(
                            $"Projection {projection.GetType()} did not increment NextSequence ({sequenceNumber}) after processing event {message.GetType()}");
                }
            }

            _nextSequenceNumber++;
        }
    }
}