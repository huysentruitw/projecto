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
using System.Threading;
using System.Threading.Tasks;

namespace Projecto.Infrastructure
{
    /// <summary>
    /// Interface for Projections.
    /// </summary>
    /// <typeparam name="TProjectContext">The type of the project context (used to pass custom information to the handler).</typeparam>
    public interface IProjection<in TProjectContext>
    {
        /// <summary>
        /// Gets the next event sequence number needed by this projection.
        /// </summary>
        int NextSequenceNumber { get; }

        /// <summary>
        /// Passes a message to a matching handler and increments <see cref="NextSequenceNumber"/>.
        /// </summary>
        /// <param name="connectionResolver">The connection resolver.</param>
        /// <param name="context">The project context (used to pass custom information to the handler).</param>
        /// <param name="message">The message.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A <see cref="Task"/> for async execution.</returns>
        Task Handle(Func<Type, object> connectionResolver, TProjectContext context, object message, CancellationToken cancellationToken);
    }
}
