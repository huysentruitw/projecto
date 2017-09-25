using System.Diagnostics.CodeAnalysis;

namespace Projecto.Tests.TestClasses
{
    [SuppressMessage("ReSharper", "UnusedParameter.Global")]
    [SuppressMessage("ReSharper", "ClassWithVirtualMembersNeverInherited.Global")]
    public class TestProjection : Projection<FakeConnection, FakeMessageEnvelope>
    {
        private readonly int? _initialNextSequence;

        public TestProjection(int? initialNextSequence = null)
        {
            _initialNextSequence = initialNextSequence;

            When<RegisteredMessageA>((conn, ctx, msg) => conn.UpdateA(ctx, msg));

            When<RegisteredMessageB>((conn, ctx, msg, cancellationToken) => conn.UpdateB(ctx, msg, cancellationToken));
        }

        public virtual void MockFetchNextSequenceNumber(FakeConnection connection) { }

        public virtual void MockIncrementSequenceNumber(FakeConnection connection, bool messageHandledByProjection) { }

        protected override int FetchNextSequenceNumber(FakeConnection connection)
        {
            MockFetchNextSequenceNumber(connection);
            return _initialNextSequence ?? base.FetchNextSequenceNumber(connection);
        }

        protected override void IncrementNextSequenceNumber(FakeConnection connection, bool messageHandledByProjection)
        {
            base.IncrementNextSequenceNumber(connection, messageHandledByProjection);
            MockIncrementSequenceNumber(connection, messageHandledByProjection);
        }
    }
}
