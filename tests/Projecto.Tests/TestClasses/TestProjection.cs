using System.Diagnostics.CodeAnalysis;

namespace Projecto.Tests.TestClasses
{
    [SuppressMessage("ReSharper", "UnusedParameter.Global")]
    [SuppressMessage("ReSharper", "ClassWithVirtualMembersNeverInherited.Global")]
    public class TestProjection : Projection<string, FakeConnection, FakeMessageEnvelope>
    {
        public TestProjection(string key)
            : base(key)
        {
            When<RegisteredMessageA>((conn, ctx, msg) => conn.UpdateA(ctx, msg));

            When<RegisteredMessageB>((conn, ctx, msg, cancellationToken) => conn.UpdateB(ctx, msg, cancellationToken));
        }
    }
}
