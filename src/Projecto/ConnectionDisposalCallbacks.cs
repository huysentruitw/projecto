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
using System.Threading.Tasks;

namespace Projecto
{
    /// <summary>
    /// Class that holds registered connection disposal callbacks.
    /// </summary>
    public class ConnectionDisposalCallbacks
    {
        private readonly Dictionary<Type, Func<object, Task>> _disposalCallbacks = new Dictionary<Type, Func<object, Task>>();

        /// <summary>
        /// Registers a disposal callback for a certain connection type.
        /// </summary>
        /// <typeparam name="TConnection">The connection type to register the callback for.</typeparam>
        /// <param name="disposalCallback">The before disposal callback.</param>
        /// <returns>The <see cref="ConnectionDisposalCallbacks"/> instance for method chaining.</returns>
        public ConnectionDisposalCallbacks BeforeDisposalOf<TConnection>(Func<TConnection, Task> disposalCallback)
        {
            if (disposalCallback == null) throw new ArgumentNullException(nameof(disposalCallback));
            if (_disposalCallbacks.ContainsKey(typeof(TConnection)))
                throw new ArgumentException($"Disposal callback for {typeof(TConnection)} already registered", nameof(TConnection));
            _disposalCallbacks.Add(typeof(TConnection), (connection) => disposalCallback((TConnection)connection));
            return this;
        }

        /// <summary>
        /// Called from <see cref="ProjectScope"/> for each connection that was resolved during the lifetime of the scope.
        /// </summary>
        /// <param name="connectionType">The connection type.</param>
        /// <param name="connection">The connection instance.</param>
        internal async Task ExecuteDisposalCallback(Type connectionType, object connection)
        {
            Func<object, Task> disposalCallback;
            if (_disposalCallbacks.TryGetValue(connectionType, out disposalCallback))
                await disposalCallback(connection).ConfigureAwait(false);
        }
    }
}
