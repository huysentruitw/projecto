using System.Threading.Tasks;

namespace Projecto.Autofac.Tests.TestClasses
{
    public class FakeProjectionA : Projection<FakeConnection, FakeMessageEnvelope> { }

    public class FakeProjectionB : Projection<FakeConnection, FakeMessageEnvelope> { }
}
