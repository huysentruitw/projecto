namespace Projecto.Tests.TestClasses
{
    public class FakeMessageEnvelope : MessageEnvelope
    {
        public FakeMessageEnvelope(int sequenceNumber, object message)
            : base(sequenceNumber, message)
        {
        }
    }
}
