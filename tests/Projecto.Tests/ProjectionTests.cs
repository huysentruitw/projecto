using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Projecto.Infrastructure;
using Projecto.Tests.TestClasses;

namespace Projecto.Tests
{
    [TestFixture]
    public class ProjectionTests
    {
        [Test]
        public void NextSequence_ValueAfterConstruction_ShouldStartAtFetchedValue()
        {
            Assert.That(new TestProjection(3).NextSequenceNumber, Is.EqualTo(3));
            Assert.That(new TestProjection().NextSequenceNumber, Is.EqualTo(1));
        }

        [Test]
        public void NextSequence_GetValueTwice_ShouldNotIncrement()
        {
            var projection = new TestProjection();
            Assert.That(projection.NextSequenceNumber, Is.EqualTo(1));
            Assert.That(projection.NextSequenceNumber, Is.EqualTo(1));
        }

        [Test]
        public async Task NextSequence_HandleMessage_ShouldIncrementNextSequence()
        {
            IProjection<FakeProjectContext> projection = new TestProjection(5);
            Assert.That(projection.NextSequenceNumber, Is.EqualTo(5));
            await projection.Handle(new FakeConnection(), new FakeProjectContext(), new MessageA(), CancellationToken.None);
            Assert.That(projection.NextSequenceNumber, Is.EqualTo(6));
        }

        [Test]
        public void ConnectionType_AfterConstruction_ShouldContainCorrectType()
        {
            IProjection<FakeProjectContext> projection = new TestProjection();
            Assert.That(projection.ConnectionType, Is.EqualTo(typeof(FakeConnection)));
        }

        [Test]
        public void When_HandleMessages_ShouldCallCorrectHandlerWithCorrectArguments()
        {
            var connectionMock = new Mock<FakeConnection>();
            var token = new CancellationToken();
            var messageA = new MessageA();
            var messageB = new MessageB();
            var contextA = new FakeProjectContext();
            var contextB = new FakeProjectContext();

            IProjection<FakeProjectContext> projection = new TestProjection();
            projection.Handle(connectionMock.Object, contextA, messageA, token);
            projection.Handle(connectionMock.Object, contextB, messageB, token);

            connectionMock.Verify(x => x.UpdateA(contextA, messageA), Times.Once);
            connectionMock.Verify(x => x.UpdateB(contextB, messageB, token), Times.Once);

            Assert.That(projection.NextSequenceNumber, Is.EqualTo(3));
        }
    }
}
