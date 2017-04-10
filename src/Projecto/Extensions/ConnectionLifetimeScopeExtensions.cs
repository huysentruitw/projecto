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
using Projecto.DependencyInjection;

namespace Projecto.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="IConnectionLifetimeScope"/>.
    /// </summary>
    internal static class ConnectionLifetimeScopeExtensions
    {
        /// <summary>
        /// Gets a factory function for resolving a connection from the <see cref="IConnectionLifetimeScope"/> for a given projection.
        /// </summary>
        /// <typeparam name="TMessageEnvelope">The type of the message envelope used to pass the message including custom information to the handler.</typeparam>
        /// <param name="scope">The <see cref="IConnectionLifetimeScope"/> to resolve the connection from.</param>
        /// <param name="projection">The <see cref="IProjection{TMessageEnvelope}"/> to resolve the connection for.</param>
        /// <returns>A factory function for resolving the connection.</returns>
        public static Func<object> GetConnectionFactoryFor<TMessageEnvelope>(this IConnectionLifetimeScope scope,
            IProjection<TMessageEnvelope> projection)
            where TMessageEnvelope : MessageEnvelope
            => () => scope.ResolveConnection(projection.ConnectionType);
    }
}
