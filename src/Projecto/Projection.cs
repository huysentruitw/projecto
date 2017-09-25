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
using Projecto.DependencyInjection;

namespace Projecto
{
    /// <summary>
    /// Base class for projections.
    /// </summary>
    /// <typeparam name="TConnection">The type of the connection (f.e. DbContext or ElasticClient) passed to message handlers and sequence number related methods.</typeparam>
    /// <typeparam name="TMessageEnvelope">The type of the message envelope used to pass the message including custom information to the handler.</typeparam>
    public abstract class Projection<TConnection, TMessageEnvelope> : Projection<TConnection, TConnection, TMessageEnvelope>
        where TConnection : class
        where TMessageEnvelope : MessageEnvelope
    {
    }

    /// <summary>
    /// Base class for projections.
    /// </summary>
    /// <typeparam name="THandleConnection">The type of the connection (f.e. DbContext or ElasticClient) passed to message handlers.</typeparam>
    /// <typeparam name="TSequenceNumberConnection">The type of the connection (f.e. DbContext or ElasticClient) passed to sequence number related methods.</typeparam>
    /// <typeparam name="TMessageEnvelope">The type of the message envelope used to pass the message including custom information to the handler.</typeparam>
    public abstract class Projection<THandleConnection, TSequenceNumberConnection, TMessageEnvelope> : IProjection<TMessageEnvelope>
        where THandleConnection : class
        where TSequenceNumberConnection : class
        where TMessageEnvelope : MessageEnvelope
    {
        /// <summary>
        /// Handler signature.
        /// </summary>
        /// <param name="connection">The connection object.</param>
        /// <param name="messageEnvelope">The message envelope.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A <see cref="Task"/> for async execution.</returns>
        private delegate Task Handler(THandleConnection connection, TMessageEnvelope messageEnvelope, CancellationToken cancellationToken);

        private int? _nextSequenceNumber;
        private readonly Dictionary<Type, Handler> _handlers = new Dictionary<Type, Handler>();

        /// <summary>
        /// Fetch the initial next sequence number, returned by <see cref="IProjection{TMessageEnvelope}.GetNextSequenceNumber"/>, needed by this projection.
        /// This method is only called once during startup, so make sure this projection is only registered with one <see cref="Projector{TMessageEnvelope}"/>.
        /// Override this method to fetch the sequence number from persistent storage.
        /// </summary>
        /// <param name="connection">The sequence number connection.</param>
        /// <returns>The next sequence number. Defaults to 1.</returns>
        protected virtual int FetchNextSequenceNumber(TSequenceNumberConnection connection) => 1;

        /// <summary>
        /// Increment the next sequence number returned by <see cref="IProjection{TMessageEnvelope}.GetNextSequenceNumber"/>.
        /// Override this method if you want to persist the new sequence number.
        /// </summary>
        /// <param name="connection">The sequence number connection.</param>
        /// <param name="messageHandledByProjection">True when the message causing the increment was actually handled by the projection. False when the message causing the increment was not handled by the projection.</param>
        protected virtual void IncrementNextSequenceNumber(TSequenceNumberConnection connection, bool messageHandledByProjection)
            => _nextSequenceNumber = GetNextSequenceNumber(() => connection) + 1;

        /// <summary>
        /// Registers a message handler for a given message type.
        /// </summary>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <param name="handler">The message handler.</param>
        protected void When<TMessage>(Func<THandleConnection, TMessageEnvelope, TMessage, Task> handler)
            => _handlers.Add(typeof(TMessage), (connection, messageEnvelope, cancellationToken) => handler(connection, messageEnvelope, (TMessage)messageEnvelope.Message));

        /// <summary>
        /// Registers a cancellable message handler for a given message type.
        /// </summary>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <param name="handler">The message handler.</param>
        protected void When<TMessage>(Func<THandleConnection, TMessageEnvelope, TMessage, CancellationToken, Task> handler)
            => _handlers.Add(typeof(TMessage), (connection, messageEnvelope, cancellationToken) => handler(connection, messageEnvelope, (TMessage)messageEnvelope.Message, cancellationToken));

        /// <summary>
        /// Gets the next event sequence number needed by this projection.
        /// </summary>
        /// <param name="lifetimeScope">The connection lifetime scope.</param>
        /// <returns>The next sequence number needed by this projection.</returns>
        int IProjection<TMessageEnvelope>.GetNextSequenceNumber(IConnectionLifetimeScope lifetimeScope)
            => GetNextSequenceNumber(lifetimeScope.ResolveConnection<TSequenceNumberConnection>);

        /// <summary>
        /// Gets the next event sequence number needed by this projection.
        /// </summary>
        /// <param name="connectionFactory">The sequence number connection factory.</param>
        /// <returns>The next sequence number needed by this projection.</returns>
        protected int GetNextSequenceNumber(Func<TSequenceNumberConnection> connectionFactory)
        {
            if (_nextSequenceNumber.HasValue) return _nextSequenceNumber.Value;
            return (int)(_nextSequenceNumber = FetchNextSequenceNumber(connectionFactory()));
        }

        /// <summary>
        /// Passes a message to a matching handler and increments the next sequence number returned by <see cref="IProjection{TMessageEnvelope}.GetNextSequenceNumber"/>.
        /// </summary>
        /// <param name="lifetimeScope">The connection lifetime scope.</param>
        /// <param name="messageEnvelope">The message envelope.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A <see cref="Task"/> for async execution.</returns>
        async Task IProjection<TMessageEnvelope>.Handle(IConnectionLifetimeScope lifetimeScope, TMessageEnvelope messageEnvelope, CancellationToken cancellationToken)
        {
            Handler handler;
            var messageHandled = false;
            if (_handlers.TryGetValue(messageEnvelope.Message.GetType(), out handler))
            {
                var connection = lifetimeScope.ResolveConnection<THandleConnection>();
                await handler(connection, messageEnvelope, cancellationToken).ConfigureAwait(false);
                messageHandled = true;
            }

            IncrementNextSequenceNumber(lifetimeScope.ResolveConnection<TSequenceNumberConnection>(), messageHandled);
        }
    }
}
