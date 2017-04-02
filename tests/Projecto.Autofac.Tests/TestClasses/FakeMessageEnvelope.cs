namespace Projecto.Autofac.Tests.TestClasses
{
    public class FakeMessageEnvelope : MessageEnvelope
    {
        public FakeMessageEnvelope(object message)
        {
            Message = message;
        }
    }
}
