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
    /// Autofac based dependency lifetime scope factory.
    /// </summary>
    internal class AutofacDependencyLifetimeScopeFactory : IDependencyLifetimeScopeFactory
    {
        private readonly Func<ILifetimeScope> _autofacLifetimeScopeFactory;

        /// <summary>
        /// Constructs a new <see cref="AutofacDependencyLifetimeScopeFactory"/> instance.
        /// </summary>
        /// <param name="autofacLifetimeScopeFactory">Factory func for creating an Autofac lifetime scope.</param>
        public AutofacDependencyLifetimeScopeFactory(Func<ILifetimeScope> autofacLifetimeScopeFactory)
        {
            if (autofacLifetimeScopeFactory == null) throw new ArgumentNullException(nameof(autofacLifetimeScopeFactory));
            _autofacLifetimeScopeFactory = autofacLifetimeScopeFactory;
        }

        /// <summary>
        /// Begins a new dependency lifetime scope.
        /// </summary>
        /// <returns>A dependency lifetime scope.</returns>
        public IDependencyLifetimeScope BeginLifetimeScope()
        {
            var lifetimeScope = new AutofacDependencyLifetimeScope(_autofacLifetimeScopeFactory());
            lifetimeScope.DependencyResolved += e => ChildScopeDependencyResolved?.Invoke(e);
            lifetimeScope.ScopeEnding += e => ChildScopeEnding?.Invoke(e);
            return lifetimeScope;
        }

        /// <summary>
        /// Event triggered when a dependency is resolved within a child lifetime scope.
        /// </summary>
        public event DependencyResolvedEvent ChildScopeDependencyResolved;

        /// <summary>
        /// Event triggered when a child lifetime scope is about to end (right before disposal).
        /// </summary>
        public event DependencyLifetimeScopeEndingEvent ChildScopeEnding;
    }
}
