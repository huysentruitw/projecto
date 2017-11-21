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

namespace Projecto
{
    internal class ProjectionPriorityComparer<TKey, TMessageEnvelope> : IComparer<IProjection<TKey, TMessageEnvelope>>
        where TKey : IEquatable<TKey>
        where TMessageEnvelope : MessageEnvelope
    {
        public int Compare(IProjection<TKey, TMessageEnvelope> x, IProjection<TKey, TMessageEnvelope> y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return 1;
            if (y == null) return -1;
            return (int) y.Priority - (int) x.Priority;
        }
    }
}
