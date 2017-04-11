using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Projecto.Tests.TestClasses;

namespace Projecto.Tests
{
    [TestFixture]
    public class ProjectionTests
    {
        private readonly Func<FakeConnection> _connectionFactory = () => new FakeConnection();

        [Test]
        public void GetNextSequenceNumber_ValueAfterConstruction_ShouldStartAtFetchedValue()
        {
            IProjection<FakeMessageEnvelope> projection = new TestProjection(3);
            Assert.That(projection.GetNextSequenceNumber(_connectionFactory), Is.EqualTo(3));
            projection = new TestProjection();
            Assert.That(projection.GetNextSequenceNumber(_connectionFactory), Is.EqualTo(1));
        }

        [Test]
        public void GetNextSequenceNumber_GetValueTwice_ShouldNotIncrement()
        {
            IProjection<FakeMessageEnvelope> projection = new TestProjection();
            Assert.That(projection.GetNextSequenceNumber(_connectionFactory), Is.EqualTo(1));
            Assert.That(projection.GetNextSequenceNumber(_connectionFactory), Is.EqualTo(1));
        }

        [Test]
        public void GetNextSequenceNumber_GetValueTwice_ShouldOnlyCallFetchNextSequenceNumberOnce()
        {
            var projectionMock = new Mock<TestProjection>(null) {CallBase = true};
            projectionMock.Setup(x => x.MockFetchNextSequenceNumber(It.IsAny<Func<FakeConnection>>()));
            IProjection<FakeMessageEnvelope> projection = projectionMock.Object;
            Assert.That(projection.GetNextSequenceNumber(_connectionFactory), Is.EqualTo(1));
            Assert.That(projection.GetNextSequenceNumber(_connectionFactory), Is.EqualTo(1));

            projectionMock.Verify(x => x.MockFetchNextSequenceNumber(It.IsAny<Func<FakeConnection>>()), Times.Once);
        }

        [Test]
        public async Task Handle_SomeMessage_ShouldIncrementNextSequenceNumber()
        {
            IProjection<FakeMessageEnvelope> projection = new TestProjection(5);
            Assert.That(projection.GetNextSequenceNumber(_connectionFactory), Is.EqualTo(5));
            await projection.Handle(_connectionFactory, new FakeMessageEnvelope(5, new RegisteredMessageA()), CancellationToken.None);
            Assert.That(projection.GetNextSequenceNumber(_connectionFactory), Is.EqualTo(6));
        }

        [Test]
        public void Handle_TwoDifferentMessages_ShouldCallCorrectHandlerWithCorrectArguments()
        {
            var connectionMock = new Mock<FakeConnection>();
            Func<object> connectionFactory = () => connectionMock.Object;
            var token = new CancellationToken();
            var messageA = new RegisteredMessageA();
            var messageB = new RegisteredMessageB();
            var messageEnvelopeA = new FakeMessageEnvelope(1, messageA);
            var messageEnvelopeB = new FakeMessageEnvelope(2, messageB);

            IProjection<FakeMessageEnvelope> projection = new TestProjection();
            projection.Handle(connectionFactory, messageEnvelopeA, token);
            projection.Handle(connectionFactory, messageEnvelopeB, token);

            connectionMock.Verify(x => x.UpdateA(messageEnvelopeA, messageA), Times.Once);
            connectionMock.Verify(x => x.UpdateB(messageEnvelopeB, messageB, token), Times.Once);

            Assert.That(projection.GetNextSequenceNumber(connectionFactory), Is.EqualTo(3));
        }

        [Test]
        public void Handle_MessageWithRegisteredHandler_ShouldCallIncrementNextSequenceNumberMethodWithMessageHandledByProjectionSetToTrue()
        {
            var projectionMock = new Mock<TestProjection>(null) { CallBase = true };
            projectionMock.Setup(x => x.MockIncrementSequenceNumber(It.IsAny<Func<FakeConnection>>(), It.IsAny<bool>()));
            var registeredMessageEnvelope = new FakeMessageEnvelope(1, new RegisteredMessageA());

            IProjection<FakeMessageEnvelope> projection = projectionMock.Object;
            projection.Handle(_connectionFactory, registeredMessageEnvelope, CancellationToken.None);

            projectionMock.Verify(x => x.MockIncrementSequenceNumber(It.IsAny<Func<FakeConnection>>(), true), Times.Once);
            projectionMock.Verify(x => x.MockIncrementSequenceNumber(It.IsAny<Func<FakeConnection>>(), false), Times.Never);
        }

        [Test]
        public void Handle_MessageWithoutRegisteredHandler_ShouldCallIncrementNextSequenceNumberMethodWithMessageHandledByProjectionSetToFalse()
        {
            var projectionMock = new Mock<TestProjection>(null) { CallBase = true };
            projectionMock.Setup(x => x.MockIncrementSequenceNumber(It.IsAny<Func<FakeConnection>>(), It.IsAny<bool>()));
            var unregisteredMessageEnvelope = new FakeMessageEnvelope(1, new UnregisteredMessage());

            IProjection<FakeMessageEnvelope> projection = projectionMock.Object;
            projection.Handle(_connectionFactory, unregisteredMessageEnvelope, CancellationToken.None);

            projectionMock.Verify(x => x.MockIncrementSequenceNumber(It.IsAny<Func<FakeConnection>>(), true), Times.Never);
            projectionMock.Verify(x => x.MockIncrementSequenceNumber(It.IsAny<Func<FakeConnection>>(), false), Times.Once);
        }
    }
}
