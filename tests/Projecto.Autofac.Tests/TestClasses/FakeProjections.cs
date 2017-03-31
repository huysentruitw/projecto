using System.Threading.Tasks;

namespace Projecto.Autofac.Tests.TestClasses
{
    public class FakeProjectionA : Projection<FakeConnection, FakeProjectContext> { }

    public class FakeProjectionB : Projection<FakeConnection, FakeProjectContext> { }
}
