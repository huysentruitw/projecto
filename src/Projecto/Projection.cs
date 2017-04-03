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
using Projecto.Infrastructure;

namespace Projecto
{
    /// <summary>
    /// Base class for projections.
    /// </summary>
    /// <typeparam name="TConnection">The type of the connection (f.e. DbContext or ElasticClient).</typeparam>
    /// <typeparam name="TMessageEnvelope">The type of the message envelope used to pass the message including custom information to the handler.</typeparam>
    public abstract class Projection<TConnection, TMessageEnvelope> : IProjection<TMessageEnvelope>
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

        private int? _nextSequenceNumber = null;
        private readonly Dictionary<Type, Handler> _handlers = new Dictionary<Type, Handler>();

        /// <summary>
        /// Gets the type of the connection required by this projection.
        /// </summary>
        public Type ConnectionType => typeof(TConnection);

        /// <summary>
        /// Gets the next event sequence number needed by this projection.
        /// </summary>
        public int NextSequenceNumber => _nextSequenceNumber ?? (int)(_nextSequenceNumber = FetchNextSequenceNumber());

        /// <summary>
        /// Fetch the initial <see cref="NextSequenceNumber"/> value needed by this projection.
        /// This method is only called once during startup, so make sure this projection is only registered with one <see cref="Projector{TMessageEnvelope}"/>.
        /// Override this method to fetch the sequence number from persistent storage.
        /// </summary>
        /// <returns>The next sequence number. Defaults to 1.</returns>
        protected virtual int FetchNextSequenceNumber() => 1;

        /// <summary>
        /// Increment the <see cref="NextSequenceNumber"/> number.
        /// Override this method if you want to persist the new sequence number.
        /// </summary>
        protected virtual void IncrementNextSequenceNumber() => _nextSequenceNumber = NextSequenceNumber + 1;

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
        /// Passes a message to a matching handler and increments <see cref="NextSequenceNumber"/>.
        /// </summary>
        /// <param name="connectionResolver">The connection resolver.</param>
        /// <param name="messageEnvelope">The message envelope.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A <see cref="Task"/> for async execution.</returns>
        async Task IProjection<TMessageEnvelope>.Handle(Func<Type, object> connectionResolver, TMessageEnvelope messageEnvelope, CancellationToken cancellationToken)
        {
            Handler handler;
            if (_handlers.TryGetValue(messageEnvelope.Message.GetType(), out handler))
            {
                var connection = (TConnection) connectionResolver(typeof(TConnection));
                await handler(connection, messageEnvelope, cancellationToken).ConfigureAwait(false);
            }

            IncrementNextSequenceNumber();
        }
    }
}
