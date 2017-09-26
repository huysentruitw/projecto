namespace Projecto.Autofac.Tests.TestClasses
{
    public class FakeProjectionA : Projection<string, FakeConnection, FakeMessageEnvelope>
    {
        public FakeProjectionA() : base("FakeProjectionA") { }
    }

    public class FakeProjectionB : Projection<string, FakeConnection, FakeMessageEnvelope>
    {
        public FakeProjectionB() : base("FakeProjectionB") { }
    }
}
