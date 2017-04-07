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
using Autofac;
using Projecto.DependencyInjection;

namespace Projecto.Autofac
{
    /// <summary>
    /// Autofac based connection lifetime scope factory.
    /// </summary>
    internal class AutofacConnectionLifetimeScopeFactory : IConnectionLifetimeScopeFactory
    {
        private readonly Func<ILifetimeScope> _createAutofacLifetimeScope;

        /// <summary>
        /// Constructs a new <see cref="AutofacConnectionLifetimeScopeFactory"/> instance.
        /// </summary>
        /// <param name="createAutofacLifetimeScope"></param>
        public AutofacConnectionLifetimeScopeFactory(Func<ILifetimeScope> createAutofacLifetimeScope)
        {
            if (createAutofacLifetimeScope == null) throw new ArgumentNullException(nameof(createAutofacLifetimeScope));
            _createAutofacLifetimeScope = createAutofacLifetimeScope;
        }

        /// <summary>
        /// Begins a new connection lifetime scope.
        /// </summary>
        /// <returns>A connection lifetime scope.</returns>
        public IConnectionLifetimeScope BeginLifetimeScope()
        {
            var connectionLifetimeScope = new AutofacConnectionLifetimeScope(_createAutofacLifetimeScope());
            connectionLifetimeScope.ConnectionResolved += e => ChildScopeConnectionResolved?.Invoke(e);
            connectionLifetimeScope.ScopeEnding += e => ChildScopeEnding?.Invoke(e);
            return connectionLifetimeScope;
        }

        /// <summary>
        /// Event triggered when a connection is resolved within a child lifetime scope.
        /// </summary>
        public event ConnectionResolvedEvent ChildScopeConnectionResolved;

        /// <summary>
        /// Event triggered when a child lifetime scope is about to end (right before disposal).
        /// </summary>
        public event ConnectionLifetimeScopeEndingEvent ChildScopeEnding;
    }
}
