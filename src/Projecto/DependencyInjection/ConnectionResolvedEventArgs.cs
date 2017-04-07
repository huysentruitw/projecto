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

namespace Projecto.DependencyInjection
{
    /// <summary>
    /// Describes the ending of a connection lifetime scope.
    /// </summary>
    public class ConnectionResolvedEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionResolvedEventArgs"/> class. 
        /// </summary>
        /// <param name="lifetimeScope">The lifetime scope where the connection was resolved.</param>
        /// <param name="connectionType">The connection type that was resolved.</param>
        /// <param name="connection">The connection instance that was resolved.</param>
        public ConnectionResolvedEventArgs(IConnectionLifetimeScope lifetimeScope, Type connectionType, object connection)
        {
            if (lifetimeScope == null) throw new ArgumentNullException(nameof(lifetimeScope));
            if (connectionType == null) throw new ArgumentNullException(nameof(connectionType));
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            LifetimeScope = lifetimeScope;
            ConnectionType = connectionType;
            Connection = connection;
        }

        /// <summary>
        /// Gets the lifetime scope.
        /// </summary>
        public IConnectionLifetimeScope LifetimeScope { get; }

        /// <summary>
        /// Gets the connection type that was resolved.
        /// </summary>
        public Type ConnectionType { get; }

        /// <summary>
        /// Gets the connection instance that was resolved.
        /// </summary>
        public object Connection { get; }
    }
}
