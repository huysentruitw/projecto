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
    /// Describes the ending of a dependency lifetime scope.
    /// </summary>
    public class DependencyResolvedEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DependencyResolvedEventArgs"/> class. 
        /// </summary>
        /// <param name="lifetimeScope">The lifetime scope where the dependency was resolved.</param>
        /// <param name="dependencyType">The dependency type that was resolved.</param>
        /// <param name="dependency">The dependency instance that was resolved.</param>
        public DependencyResolvedEventArgs(IDependencyLifetimeScope lifetimeScope, Type dependencyType, object dependency)
        {
            if (lifetimeScope == null) throw new ArgumentNullException(nameof(lifetimeScope));
            if (dependencyType == null) throw new ArgumentNullException(nameof(dependencyType));
            if (dependency == null) throw new ArgumentNullException(nameof(dependency));
            LifetimeScope = lifetimeScope;
            DependencyType = dependencyType;
            Dependency = dependency;
        }

        /// <summary>
        /// Gets the lifetime scope.
        /// </summary>
        public IDependencyLifetimeScope LifetimeScope { get; }

        /// <summary>
        /// Gets the dependency type that was resolved.
        /// </summary>
        public Type DependencyType { get; }

        /// <summary>
        /// Gets the dependency instance that was resolved.
        /// </summary>
        public object Dependency { get; }
    }
}
