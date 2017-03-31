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

namespace Projecto.Autofac
{
    /// <summary>
    /// Autofac based project scope.
    /// </summary>
    internal class AutofacProjectScope : ProjectScope
    {
        private readonly ILifetimeScope _lifetimeScope;

        /// <summary>
        /// Project scope constructor.
        /// </summary>
        public AutofacProjectScope(Func<ILifetimeScope> lifetimeScopeFactory)
        {
            if (lifetimeScopeFactory == null) throw new ArgumentNullException(nameof(lifetimeScopeFactory));
            _lifetimeScope = lifetimeScopeFactory();
        }

        /// <summary>
        /// Disposes the project scope.
        /// </summary>
        public override void Dispose() => _lifetimeScope.Dispose();

        /// <summary>
        /// Resolves a connection from the scope.
        /// </summary>
        /// <param name="connectionType">The connection type.</param>
        /// <returns>The connection instance.</returns>
        public override object ResolveConnection(Type connectionType) => _lifetimeScope.Resolve(connectionType);
    }
}
