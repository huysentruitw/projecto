using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Projecto.DependencyInjection;
using Projecto.Tests.TestClasses;

namespace Projecto.Tests
{
    [TestFixture]
    public class ProjectionTests
    {
        private Mock<FakeConnection> _connectionMock;
        private IConnectionLifetimeScope _scope;

        [SetUp]
        public void SetUp()
        {
            _connectionMock = new Mock<FakeConnection>();

            var scopeMock = new Mock<IConnectionLifetimeScope>();
            scopeMock
                .Setup(x => x.ResolveConnection<FakeConnection>())
                .Returns(_connectionMock.Object);

            _scope = scopeMock.Object;
        }

        [Test]
        public void GetNextSequenceNumber_ValueAfterConstruction_ShouldStartAtFetchedValue()
        {
            IProjection<FakeMessageEnvelope> projection = new TestProjection(3);
            Assert.That(projection.GetNextSequenceNumber(_scope), Is.EqualTo(3));
            projection = new TestProjection();
            Assert.That(projection.GetNextSequenceNumber(_scope), Is.EqualTo(1));
        }

        [Test]
        public void GetNextSequenceNumber_GetValueTwice_ShouldNotIncrement()
        {
            IProjection<FakeMessageEnvelope> projection = new TestProjection();
            Assert.That(projection.GetNextSequenceNumber(_scope), Is.EqualTo(1));
            Assert.That(projection.GetNextSequenceNumber(_scope), Is.EqualTo(1));
        }

        [Test]
        public void GetNextSequenceNumber_GetValueTwice_ShouldOnlyCallFetchNextSequenceNumberOnce()
        {
            var projectionMock = new Mock<TestProjection>(null) {CallBase = true};
            projectionMock.Setup(x => x.MockFetchNextSequenceNumber(It.IsAny<FakeConnection>()));
            IProjection<FakeMessageEnvelope> projection = projectionMock.Object;
            Assert.That(projection.GetNextSequenceNumber(_scope), Is.EqualTo(1));
            Assert.That(projection.GetNextSequenceNumber(_scope), Is.EqualTo(1));

            projectionMock.Verify(x => x.MockFetchNextSequenceNumber(It.IsAny<FakeConnection>()), Times.Once);
        }

        [Test]
        public async Task Handle_SomeMessage_ShouldIncrementNextSequenceNumber()
        {
            IProjection<FakeMessageEnvelope> projection = new TestProjection(5);
            Assert.That(projection.GetNextSequenceNumber(_scope), Is.EqualTo(5));
            await projection.Handle(_scope, new FakeMessageEnvelope(5, new RegisteredMessageA()), CancellationToken.None);
            Assert.That(projection.GetNextSequenceNumber(_scope), Is.EqualTo(6));
        }

        [Test]
        public void Handle_TwoDifferentMessages_ShouldCallCorrectHandlerWithCorrectArguments()
        {
            var token = new CancellationToken();
            var messageA = new RegisteredMessageA();
            var messageB = new RegisteredMessageB();
            var messageEnvelopeA = new FakeMessageEnvelope(1, messageA);
            var messageEnvelopeB = new FakeMessageEnvelope(2, messageB);

            IProjection<FakeMessageEnvelope> projection = new TestProjection();
            projection.Handle(_scope, messageEnvelopeA, token);
            projection.Handle(_scope, messageEnvelopeB, token);

            _connectionMock.Verify(x => x.UpdateA(messageEnvelopeA, messageA), Times.Once);
            _connectionMock.Verify(x => x.UpdateB(messageEnvelopeB, messageB, token), Times.Once);

            Assert.That(projection.GetNextSequenceNumber(_scope), Is.EqualTo(3));
        }

        [Test]
        public void Handle_MessageWithRegisteredHandler_ShouldCallIncrementNextSequenceNumberMethodWithMessageHandledByProjectionSetToTrue()
        {
            var projectionMock = new Mock<TestProjection>(null) { CallBase = true };
            projectionMock.Setup(x => x.MockIncrementSequenceNumber(It.IsAny<FakeConnection>(), It.IsAny<bool>()));
            var registeredMessageEnvelope = new FakeMessageEnvelope(1, new RegisteredMessageA());

            IProjection<FakeMessageEnvelope> projection = projectionMock.Object;
            projection.Handle(_scope, registeredMessageEnvelope, CancellationToken.None);

            projectionMock.Verify(x => x.MockIncrementSequenceNumber(It.IsAny<FakeConnection>(), true), Times.Once);
            projectionMock.Verify(x => x.MockIncrementSequenceNumber(It.IsAny<FakeConnection>(), false), Times.Never);
        }

        [Test]
        public void Handle_MessageWithoutRegisteredHandler_ShouldCallIncrementNextSequenceNumberMethodWithMessageHandledByProjectionSetToFalse()
        {
            var projectionMock = new Mock<TestProjection>(null) { CallBase = true };
            projectionMock.Setup(x => x.MockIncrementSequenceNumber(It.IsAny<FakeConnection>(), It.IsAny<bool>()));
            var unregisteredMessageEnvelope = new FakeMessageEnvelope(1, new UnregisteredMessage());

            IProjection<FakeMessageEnvelope> projection = projectionMock.Object;
            projection.Handle(_scope, unregisteredMessageEnvelope, CancellationToken.None);

            projectionMock.Verify(x => x.MockIncrementSequenceNumber(It.IsAny<FakeConnection>(), true), Times.Never);
            projectionMock.Verify(x => x.MockIncrementSequenceNumber(It.IsAny<FakeConnection>(), false), Times.Once);
        }
    }
}
