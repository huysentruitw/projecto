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
using Projecto.Infrastructure;

namespace Projecto
{
    /// <summary>
    /// Builder for constructing a <see cref="Projector{TProjectContext}"/> instance.
    /// </summary>
    /// <typeparam name="TProjectContext">The type of the project context (used to pass custom information to the handler).</typeparam>
    public class ProjectorBuilder<TProjectContext>
    {
        private readonly HashSet<IProjection<TProjectContext>> _projections = new HashSet<IProjection<TProjectContext>>();
        private ProjectScopeFactory<TProjectContext> _projectScopeFactory;

        /// <summary>
        /// Registers a projection.
        /// </summary>
        /// <typeparam name="TConnection">The type of the connection needed by the projection.</typeparam>
        /// <param name="projection">The projection to register.</param>
        /// <returns><see cref="ProjectorBuilder{TProjectContext}"/> for method chaining.</returns>
        public ProjectorBuilder<TProjectContext> Register<TConnection>(Projection<TConnection, TProjectContext> projection)
        {
            if (projection == null) throw new ArgumentNullException(nameof(projection));
            _projections.Add(projection);
            return this;
        }

        /// <summary>
        /// Registers multiple projections.
        /// </summary>
        /// <typeparam name="TConnection">The type of the connection needed by the projections.</typeparam>
        /// <param name="projections">The projections.</param>
        /// <returns><see cref="ProjectorBuilder{TProjectContext}"/> for method chaining.</returns>
        public ProjectorBuilder<TProjectContext> Register<TConnection>(IEnumerable<Projection<TConnection, TProjectContext>> projections)
        {
            if (projections == null) throw new ArgumentNullException(nameof(projections));
            foreach (var projection in projections) Register(projection);
            return this;
        }

        /// <summary>
        /// Sets the project scope factory.
        /// </summary>
        /// <param name="projectScopeFactory">The project scope factory.</param>
        /// <returns><see cref="ProjectorBuilder{TProjectContext}"/> for method chaining.</returns>
        public ProjectorBuilder<TProjectContext> SetProjectScopeFactory(ProjectScopeFactory<TProjectContext> projectScopeFactory)
        {
            if (projectScopeFactory == null) throw new ArgumentNullException(nameof(projectScopeFactory));
            _projectScopeFactory = projectScopeFactory;
            return this;
        }

        /// <summary>
        /// Build a <see cref="Projector{TProjectContext}"/> instance.
        /// </summary>
        /// <returns>The <see cref="Projector{TProjectContext}"/> instance.</returns>
        public Projector<TProjectContext> Build() => new Projector<TProjectContext>(_projections, _projectScopeFactory);
    }
}
