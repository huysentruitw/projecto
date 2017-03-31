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
using System.Threading;

namespace Projecto
{
    /// <summary>
    /// Abstract class that defines a project scope.
    /// A project scope is created for each call to <see cref="Projector{TProjectContext}.Project(int, TProjectContext, object, CancellationToken)"/> method, the scope gets disposed before leaving the method.
    /// </summary>
    public abstract class ProjectScope : IDisposable
    {
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
    }
}
