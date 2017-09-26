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

namespace Projecto
{
    /// <summary>
    /// Interface for projections.
    /// </summary>
    /// <typeparam name="TKey">The type of the key that uniquely identifies the projection.</typeparam>
    /// <typeparam name="TMessageEnvelope">The type of the message envelope used to pass the message including custom information to the handler.</typeparam>
    public interface IProjection<TKey, in TMessageEnvelope>
        where TKey : IEquatable<TKey>
        where TMessageEnvelope : MessageEnvelope
    {
        /// <summary>
        /// Gets the key that uniquely identifies this projection.
        /// </summary>
        TKey Key { get; }

        /// <summary>
        /// Gets the type of the connection required by this projection.
        /// </summary>
        Type ConnectionType { get; }

        /// <summary>
        /// Passes a message to a matching handler, if any.
        /// </summary>
        /// <param name="connectionFactory">The connection factory.</param>
        /// <param name="messageEnvelope">The message envelope.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>True when the message was handled by the projection, false when not.</returns>
        Task<bool> Handle(Func<object> connectionFactory, TMessageEnvelope messageEnvelope, CancellationToken cancellationToken);
    }
}
