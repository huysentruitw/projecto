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
    /// <see cref="ProjectorBuilder{TProjectContext}"/> extension methods.
    /// </summary>
    public static class ProjectorBuilderExtensions
    {
        /// <summary>
        /// Registers all projections that are registered as <see cref="IProjection{TProjectContext}"/> on the Autofac container.
        /// </summary>
        /// <typeparam name="TProjectContext">The type of the project context (used to pass custom information to the handler).</typeparam>
        /// <param name="builder">The builder.</param>
        /// <param name="componentContext">The Autofac component context.</param>
        /// <returns></returns>
        public static ProjectorBuilder<TProjectContext>
            UseAutofac<TProjectContext>(this ProjectorBuilder<TProjectContext> builder, IComponentContext componentContext)
        {
            var projections = componentContext.Resolve<IEnumerable<IProjection<TProjectContext>>>();
            builder.Register(projections);

            var lifetimeScopeFactory = componentContext.Resolve<Func<ILifetimeScope>>();
            builder.SetProjectScopeFactory((_, __) => new AutofacProjectScope(lifetimeScopeFactory));
            return builder;
        }
    }
}
