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
    /// Builder for constructing a <see cref="Projector{TProjectionKey, TMessageEnvelope, TNextSequenceNumberRepository}"/> instance.
    /// </summary>
    /// <typeparam name="TProjectionKey">The type of the key that uniquely identifies a projection.</typeparam>
    /// <typeparam name="TMessageEnvelope">The type of the message envelope used to pass the message including custom information to the handler.</typeparam>
    public class ProjectorBuilder<TProjectionKey, TMessageEnvelope>
        where TProjectionKey : IEquatable<TProjectionKey>
        where TMessageEnvelope : MessageEnvelope
    {
        private readonly HashSet<IProjection<TProjectionKey, TMessageEnvelope>> _projections = new HashSet<IProjection<TProjectionKey, TMessageEnvelope>>();
        private IDependencyLifetimeScopeFactory _dependencyLifetimeScopeFactory;

        /// <summary>
        /// Registers a projection.
        /// </summary>
        /// <param name="projection">The projection to register.</param>
        /// <returns><see cref="ProjectorBuilder{TProjectionKey, TMessageEnvelope}"/> for method chaining.</returns>
        public ProjectorBuilder<TProjectionKey, TMessageEnvelope> Register(IProjection<TProjectionKey, TMessageEnvelope> projection)
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
        /// <returns><see cref="ProjectorBuilder{TProjectionKey, TMessageEnvelope}"/> for method chaining.</returns>
        public ProjectorBuilder<TProjectionKey, TMessageEnvelope> Register(IEnumerable<IProjection<TProjectionKey, TMessageEnvelope>> projections)
        {
            if (projections == null) throw new ArgumentNullException(nameof(projections));
            foreach (var projection in projections) Register(projection);
            return this;
        }

        /// <summary>
        /// Sets the dependency lifetime scope factory.
        /// This factory is used for creating <see cref="IDependencyLifetimeScope"/> instances from where connections and the <see cref="INextSequenceNumberRepository{TProjectionKey}"/> are resolved during message projection.
        /// </summary>
        /// <param name="factory">The dependency lifetime scope factory.</param>
        /// <returns><see cref="ProjectorBuilder{TProjectionKey, TMessageEnvelope}"/> for method chaining.</returns>
        [SuppressMessage("ReSharper", "UnusedMethodReturnValue.Global")]
        public ProjectorBuilder<TProjectionKey, TMessageEnvelope> SetDependencyLifetimeScopeFactory(IDependencyLifetimeScopeFactory factory)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            _dependencyLifetimeScopeFactory = factory;
            return this;
        }

        /// <summary>
        /// Build a <see cref="Projector{TProjectionKey, TMessageEnvelope, TNextSequenceNumberRepository}"/> instance.
        /// </summary>
        /// <typeparam name="TNextSequenceNumberRepository">The type of the <see cref="INextSequenceNumberRepository{TProjectionKey}"/> implementation to use.</typeparam>
        /// <returns>The <see cref="Projector{TProjectionKey, TMessageEnvelope, TNextSequenceNumberRepository}"/> instance.</returns>
        public Projector<TProjectionKey, TMessageEnvelope, TNextSequenceNumberRepository> Build<TNextSequenceNumberRepository>()
            where TNextSequenceNumberRepository : INextSequenceNumberRepository<TProjectionKey>
            => new Projector<TProjectionKey, TMessageEnvelope, TNextSequenceNumberRepository>(_projections, _dependencyLifetimeScopeFactory);
    }
}
