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
    /// Abstract class that defines a project scope.
    /// A project scope is created for each call to <see cref="Projector{TMessageEnvelope}.Project(TMessageEnvelope, CancellationToken)"/> method, the scope gets disposed before leaving the method.
    /// </summary>
    public abstract class ProjectScope : IDisposable
    {
        private readonly ConnectionDisposalCallbacks _connectionDisposalCallbacks = null;
        private readonly HashSet<Tuple<Type, object>> _resolvedConnections = new HashSet<Tuple<Type, object>>();

        /// <summary>
        /// Constructs a <see cref="ProjectScope"/> instance with a <see cref="ConnectionDisposalCallbacks"/> object.
        /// </summary>
        /// <param name="connectionDisposalCallbacks">Optional instance of <see cref="ConnectionDisposalCallbacks"/> that holds registered connection disposal callbacks.</param>
        public ProjectScope(ConnectionDisposalCallbacks connectionDisposalCallbacks = null)
        {
            _connectionDisposalCallbacks = connectionDisposalCallbacks;
        }

        /// <summary>
        /// Called when the project scope gets disposed.
        /// </summary>
        public abstract void Dispose();

        /// <summary>
        /// Called when a certain connection needs to be resolved.
        /// </summary>
        /// <param name="connectionType">The type of the connection to resolve.</param>
        /// <returns>The connection instance.</returns>
        public abstract object ResolveConnection(Type connectionType);

        internal object InternalResolveConnection(Type connectionType)
        {
            var connection = ResolveConnection(connectionType);
            if (connection != null) _resolvedConnections.Add(new Tuple<Type, object>(connectionType, connection));
            return connection;
        }

        internal virtual async Task BeforeDispose()
        {
            if (_connectionDisposalCallbacks != null)
                foreach (var resolvedConnection in _resolvedConnections)
                    await _connectionDisposalCallbacks.ExecuteDisposalCallback(resolvedConnection.Item1, resolvedConnection.Item2).ConfigureAwait(false);
        }
    }
}
