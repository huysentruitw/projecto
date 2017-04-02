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
using Projecto.Infrastructure;

namespace Projecto.Autofac
{
    /// <summary>
    /// <see cref="ProjectorBuilder{TMessageEnvelope}"/> extension methods.
    /// </summary>
    public static class ProjectorBuilderExtensions
    {
        /// <summary>
        /// Registers all projections that are registered as <see cref="IProjection{TMessageEnvelope}"/> on the Autofac container.
        /// </summary>
        /// <typeparam name="TMessageEnvelope">The type of the message envelope used to pass the message including custom information to the handler.</typeparam>
        /// <param name="builder">The builder.</param>
        /// <param name="componentContext">The Autofac component context.</param>
        /// <returns><see cref="ProjectorBuilder{TMessageEnvelope}"/> for method chaining.</returns>
        public static ProjectorBuilder<TMessageEnvelope>
            RegisterFromAutofac<TMessageEnvelope>(this ProjectorBuilder<TMessageEnvelope> builder, IComponentContext componentContext)
            where TMessageEnvelope : MessageEnvelope
        {
            var projections = componentContext.Resolve<IEnumerable<IProjection<TMessageEnvelope>>>();
            builder.Register(projections);
            return builder;
        }

        /// <summary>
        /// Configures the builder to resolve requested connection instances from the Autofac component scope.
        /// </summary>
        /// <typeparam name="TMessageEnvelope">The type of the message envelope used to pass the message including custom information to the handler.</typeparam>
        /// <param name="builder">The builder.</param>
        /// <param name="componentContext">The Autofac component context.</param>
        /// <param name="connectionDisposalCallbacks">Optional <see cref="ConnectionDisposalCallbacks"/> instance.</param>
        /// <returns><see cref="ProjectorBuilder{TMessageEnvelope}"/> for method chaining.</returns>
        public static ProjectorBuilder<TMessageEnvelope>
            UseAutofacProjectScopeFactory<TMessageEnvelope>(this ProjectorBuilder<TMessageEnvelope> builder, IComponentContext componentContext, ConnectionDisposalCallbacks connectionDisposalCallbacks = null)
            where TMessageEnvelope : MessageEnvelope
        {
            var lifetimeScopeFactory = componentContext.Resolve<Func<ILifetimeScope>>();
            builder.SetProjectScopeFactory(_ => new AutofacProjectScope(() => lifetimeScopeFactory().BeginLifetimeScope(), connectionDisposalCallbacks));
            return builder;
        }
    }
}
