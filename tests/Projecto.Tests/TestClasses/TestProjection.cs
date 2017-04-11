using System;
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

        public virtual void MockFetchNextSequenceNumber(Func<FakeConnection> connectionFactory) { }

        public virtual void MockIncrementSequenceNumber(Func<FakeConnection> connectionFactory, bool messageHandledByProjection) { }

        protected override int FetchNextSequenceNumber(Func<FakeConnection> connectionFactory)
        {
            MockFetchNextSequenceNumber(connectionFactory);
            return _initialNextSequence ?? base.FetchNextSequenceNumber(connectionFactory);
        }

        protected override void IncrementNextSequenceNumber(Func<FakeConnection> connectionFactory, bool messageHandledByProjection)
        {
            base.IncrementNextSequenceNumber(connectionFactory, messageHandledByProjection);
            MockIncrementSequenceNumber(connectionFactory, messageHandledByProjection);
        }
    }
}
