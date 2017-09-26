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
using System.Threading.Tasks;

namespace Projecto
{
    /// <summary>
    /// Interface for a repository that keeps track of the next sequence number for projections.
    /// </summary>
    /// <typeparam name="TProjectionKey">The type of the key that uniquely identifies a projection.</typeparam>
    public interface INextSequenceNumberRepository<TProjectionKey>
        where TProjectionKey : IEquatable<TProjectionKey>
    {
        /// <summary>
        /// Fetches the next sequence number for the given projections.
        /// </summary>
        /// <param name="projectionKeys">The keys that uniquely identify the projections.</param>
        /// <returns>The next sequence number for the projections.</returns>
        Task<IReadOnlyDictionary<TProjectionKey, int>> Fetch(ISet<TProjectionKey> projectionKeys);

        /// <summary>
        /// Stores the next sequence number for the given projections.
        /// </summary>
        /// <param name="nextSequenceNumbers">The next sequence number for the given projections.</param>
        /// <returns>A <see cref="Task"/> for async execution.</returns>
        Task Store(IReadOnlyDictionary<TProjectionKey, int> nextSequenceNumbers);
    }
}
