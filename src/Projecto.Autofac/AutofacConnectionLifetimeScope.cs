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
using Autofac;
using Projecto.DependencyInjection;

namespace Projecto.Autofac
{
    /// <summary>
    /// Autofac based connection lifetime scope.
    /// </summary>
    internal class AutofacConnectionLifetimeScope : IConnectionLifetimeScope
    {
        private readonly ILifetimeScope _lifetimeScope;
        private bool _disposed;

        /// <summary>
        /// Creates a new <see cref="AutofacConnectionLifetimeScope"/> instance.
        /// </summary>
        /// <param name="lifetimeScope">A new Autofac lifetime scope.</param>
        public AutofacConnectionLifetimeScope(ILifetimeScope lifetimeScope)
        {
            if (lifetimeScope == null) throw new ArgumentNullException(nameof(lifetimeScope));
            _lifetimeScope = lifetimeScope;
        }

        /// <summary>
        /// Disposes the lifetime scope.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            ScopeEnding?.Invoke(new ConnectionLifetimeScopeEndingEventArgs(this));
            _lifetimeScope.Dispose();
            _disposed = true;
        }

        /// <summary>
        /// Property dictionary for sharing data between different event handlers.
        /// </summary>
        public IDictionary<object, object> Properties { get; } = new Dictionary<object, object>();

        /// <summary>
        /// Resolves a connection.
        /// </summary>
        /// <typeparam name="TConnection">The type of the connection.</typeparam>
        /// <returns>An instance of the requested connection type or null when the type was not found.</returns>
        public TConnection ResolveConnection<TConnection>()
        {
            var connection = _lifetimeScope.Resolve<TConnection>();
            if (connection != null) ConnectionResolved?.Invoke(new ConnectionResolvedEventArgs(this, typeof(TConnection), connection));
            return connection;
        }

        /// <summary>
        /// Event triggered when a connection was resolved from the lifetime scope.
        /// </summary>
        public event ConnectionResolvedEvent ConnectionResolved;

        /// <summary>
        /// Event triggered when the lifetime scope is about to end (right before disposal).
        /// </summary>
        public event ConnectionLifetimeScopeEndingEvent ScopeEnding;
    }
}
