using System;

namespace Projecto.Tests.TestClasses
{
    public class FakeMessageEnvelope : MessageEnvelope
    {
        public FakeMessageEnvelope(object message)
        {
            Message = message;
        }

        public string OriginatingCommandId { get; } = Guid.NewGuid().ToString("N");

        public DateTime DateCreated { get; } = DateTime.UtcNow;
    }
}
