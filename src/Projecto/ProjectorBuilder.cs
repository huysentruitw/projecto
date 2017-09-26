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
using System.Diagnostics.CodeAnalysis;
using Projecto.DependencyInjection;

namespace Projecto
{
    /// <summary>
    /// Builder for constructing a <see cref="Projector{TMessageEnvelope}"/> instance.
    /// </summary>
    /// <typeparam name="TMessageEnvelope">The type of the message envelope used to pass the message including custom information to the handler.</typeparam>
    public class ProjectorBuilder<TMessageEnvelope>
        where TMessageEnvelope : MessageEnvelope
    {
        private readonly HashSet<IProjection<TMessageEnvelope>> _projections = new HashSet<IProjection<TMessageEnvelope>>();
        private IDependencyLifetimeScopeFactory _dependencyLifetimeScopeFactory;

        /// <summary>
        /// Registers a projection.
        /// </summary>
        /// <param name="projection">The projection to register.</param>
        /// <returns><see cref="ProjectorBuilder{TMessageEnvelope}"/> for method chaining.</returns>
        public ProjectorBuilder<TMessageEnvelope> Register(IProjection<TMessageEnvelope> projection)
        {
            if (projection == null) throw new ArgumentNullException(nameof(projection));
            if (_projections.Contains(projection)) throw new ArgumentException("Projection already registered", nameof(projection));
            _projections.Add(projection);
            return this;
        }

        /// <summary>
        /// Registers multiple projections.
        /// </summary>
        /// <param name="projections">The projections.</param>
        /// <returns><see cref="ProjectorBuilder{TMessageEnvelope}"/> for method chaining.</returns>
        public ProjectorBuilder<TMessageEnvelope> Register(IEnumerable<IProjection<TMessageEnvelope>> projections)
        {
            if (projections == null) throw new ArgumentNullException(nameof(projections));
            foreach (var projection in projections) Register(projection);
            return this;
        }

        /// <summary>
        /// Sets the dependency lifetime scope factory.
        /// </summary>
        /// <param name="factory">The dependency lifetime scope factory.</param>
        /// <returns><see cref="ProjectorBuilder{TMessageEnvelope}"/> for method chaining.</returns>
        [SuppressMessage("ReSharper", "UnusedMethodReturnValue.Global")]
        public ProjectorBuilder<TMessageEnvelope> SetDependencyLifetimeScopeFactory(IDependencyLifetimeScopeFactory factory)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            _dependencyLifetimeScopeFactory = factory;
            return this;
        }

        /// <summary>
        /// Build a <see cref="Projector{TMessageEnvelope}"/> instance.
        /// </summary>
        /// <returns>The <see cref="Projector{TMessageEnvelope}"/> instance.</returns>
        public Projector<TMessageEnvelope> Build() => new Projector<TMessageEnvelope>(_projections, _dependencyLifetimeScopeFactory);
    }
}
