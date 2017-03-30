using System.Threading;
using System.Threading.Tasks;

namespace Projecto.Tests.TestClasses
{
    public class FakeConnection
    {
        public virtual Task UpdateA(FakeProjectContext ctx, MessageA msg) => Task.FromResult(0);
        public virtual Task UpdateB(FakeProjectContext ctx, MessageB msg, CancellationToken token) => Task.FromResult(0);
    }
}
