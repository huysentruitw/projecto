namespace Projecto.Tests.TestClasses
{
    public class TestProjection : Projection<FakeConnection, FakeMessageEnvelope>
    {
        private readonly int? _initialNextSequence;

        public TestProjection(int? initialNextSequence = null)
        {
            _initialNextSequence = initialNextSequence;

            When<RegisteredMessageA>((conn, ctx, msg) => conn.UpdateA(ctx, msg));

            When<RegisteredMessageB>((conn, ctx, msg, cancellationToken) => conn.UpdateB(ctx, msg, cancellationToken));
        }

        public virtual void MockIncrementSequenceNumber(bool messageHandledByProjection) { }

        protected override int FetchNextSequenceNumber() => _initialNextSequence ?? base.FetchNextSequenceNumber();

        protected override void IncrementNextSequenceNumber(bool messageHandledByProjection)
        {
            base.IncrementNextSequenceNumber(messageHandledByProjection);
            MockIncrementSequenceNumber(messageHandledByProjection);
        }
    }
}
