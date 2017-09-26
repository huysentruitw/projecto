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
using System.Threading;
using System.Threading.Tasks;

namespace Projecto
{
    /// <summary>
    /// Base class for projections.
    /// </summary>
    /// <typeparam name="TKey">The type of the key that uniquely identifies the projection.</typeparam>
    /// <typeparam name="TConnection">The type of the connection (f.e. DbContext or ElasticClient).</typeparam>
    /// <typeparam name="TMessageEnvelope">The type of the message envelope used to pass the message including custom information to the handler.</typeparam>
    public abstract class Projection<TKey, TConnection, TMessageEnvelope> : IProjection<TKey, TMessageEnvelope>
        where TKey : IEquatable<TKey>
        where TConnection : class
        where TMessageEnvelope : MessageEnvelope
    {
        /// <summary>
        /// Handler signature.
        /// </summary>
        /// <param name="connection">The connection object.</param>
        /// <param name="messageEnvelope">The message envelope.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A <see cref="Task"/> for async execution.</returns>
        private delegate Task Handler(TConnection connection, TMessageEnvelope messageEnvelope, CancellationToken cancellationToken);

        private readonly Dictionary<Type, Handler> _handlers = new Dictionary<Type, Handler>();

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="key">The key that uniquely identifies this projection.</param>
        protected Projection(TKey key)
        {
            Key = key;
        }

        /// <summary>
        /// Gets the key that uniquely identifies this projection.
        /// </summary>
        public TKey Key { get; }

        /// <summary>
        /// Gets the type of the connection required by this projection.
        /// </summary>
        public Type ConnectionType => typeof(TConnection);

        /// <summary>
        /// Registers a message handler for a given message type.
        /// </summary>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <param name="handler">The message handler.</param>
        protected void When<TMessage>(Func<TConnection, TMessageEnvelope, TMessage, Task> handler)
            => _handlers.Add(typeof(TMessage), (connection, messageEnvelope, cancellationToken) => handler(connection, messageEnvelope, (TMessage)messageEnvelope.Message));

        /// <summary>
        /// Registers a cancellable message handler for a given message type.
        /// </summary>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <param name="handler">The message handler.</param>
        protected void When<TMessage>(Func<TConnection, TMessageEnvelope, TMessage, CancellationToken, Task> handler)
            => _handlers.Add(typeof(TMessage), (connection, messageEnvelope, cancellationToken) => handler(connection, messageEnvelope, (TMessage)messageEnvelope.Message, cancellationToken));

        /// <summary>
        /// Passes a message to a matching handler, if any.
        /// </summary>
        /// <param name="connectionFactory">The connection factory.</param>
        /// <param name="messageEnvelope">The message envelope.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>True when the message was handled by the projection, false when not.</returns>
        async Task<bool> IProjection<TKey, TMessageEnvelope>.Handle(Func<object> connectionFactory, TMessageEnvelope messageEnvelope, CancellationToken cancellationToken)
        {
            Handler handler;
            if (_handlers.TryGetValue(messageEnvelope.Message.GetType(), out handler))
            {
                var connection = (TConnection) connectionFactory();
                await handler(connection, messageEnvelope, cancellationToken).ConfigureAwait(false);
                return true;
            }

            return false;
        }
    }
}
