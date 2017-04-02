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

namespace Projecto
{
    /// <summary>
    /// Project scope factory delegate.
    /// </summary>
    /// <typeparam name="TMessageEnvelope">The type of the message envelope used to pass the message including custom information to the handler.</typeparam>
    /// <param name="messageEnveloope">The message instance.</param>
    /// <returns>A project scope instance.</returns>
    public delegate ProjectScope ProjectScopeFactory<in TMessageEnvelope>(TMessageEnvelope messageEnveloope)
        where TMessageEnvelope : MessageEnvelope;
}
