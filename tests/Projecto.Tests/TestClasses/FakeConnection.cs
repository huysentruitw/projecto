using System.Threading;
using System.Threading.Tasks;

namespace Projecto.Tests.TestClasses
{
    public class FakeConnection
    {
        public virtual Task UpdateA(FakeMessageEnvelope ctx, RegisteredMessageA msg) => Task.FromResult(0);
        public virtual Task UpdateB(FakeMessageEnvelope ctx, RegisteredMessageB msg, CancellationToken token) => Task.FromResult(0);
    }
}
