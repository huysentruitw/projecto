namespace Projecto.Tests.TestClasses
{
    public class TestProjection : Projection<FakeConnection, FakeProjectContext>
    {
        private readonly int? _initialNextSequence;

        public TestProjection(int? initialNextSequence = null)
        {
            _initialNextSequence = initialNextSequence;

            When<MessageA>((conn, ctx, msg) => conn.UpdateA(ctx, msg));

            When<MessageB>((conn, ctx, msg, cancellationToken) => conn.UpdateB(ctx, msg, cancellationToken));
        }

        protected override int FetchNextSequenceNumber() => _initialNextSequence ?? base.FetchNextSequenceNumber();
    }
}
