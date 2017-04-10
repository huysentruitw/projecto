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
        [Test]
        public void NextSequence_ValueAfterConstruction_ShouldStartAtFetchedValue()
        {
            Assert.That(new TestProjection(3).GetNextSequenceNumber, Is.EqualTo(3));
            Assert.That(new TestProjection().GetNextSequenceNumber, Is.EqualTo(1));
        }

        [Test]
        public void NextSequence_GetValueTwice_ShouldNotIncrement()
        {
            var projection = new TestProjection();
            Assert.That(projection.GetNextSequenceNumber, Is.EqualTo(1));
            Assert.That(projection.GetNextSequenceNumber, Is.EqualTo(1));
        }

        [Test]
        public async Task Handle_SomeMessage_ShouldIncrementNextSequence()
        {
            IProjection<FakeMessageEnvelope> projection = new TestProjection(5);
            Assert.That(projection.GetNextSequenceNumber, Is.EqualTo(5));
            await projection.Handle(() => new FakeConnection(), new FakeMessageEnvelope(5, new RegisteredMessageA()), CancellationToken.None);
            Assert.That(projection.GetNextSequenceNumber, Is.EqualTo(6));
        }

        [Test]
        public void Handle_TwoDifferentMessages_ShouldCallCorrectHandlerWithCorrectArguments()
        {
            var connectionMock = new Mock<FakeConnection>();
            var token = new CancellationToken();
            var messageA = new RegisteredMessageA();
            var messageB = new RegisteredMessageB();
            var messageEnvelopeA = new FakeMessageEnvelope(1, messageA);
            var messageEnvelopeB = new FakeMessageEnvelope(2, messageB);

            IProjection<FakeMessageEnvelope> projection = new TestProjection();
            projection.Handle(() => connectionMock.Object, messageEnvelopeA, token);
            projection.Handle(() => connectionMock.Object, messageEnvelopeB, token);

            connectionMock.Verify(x => x.UpdateA(messageEnvelopeA, messageA), Times.Once);
            connectionMock.Verify(x => x.UpdateB(messageEnvelopeB, messageB, token), Times.Once);

            Assert.That(projection.GetNextSequenceNumber, Is.EqualTo(3));
        }

        [Test]
        public void Handle_MessageWithRegisteredHandler_ShouldCallIncrementNextSequenceNumberMethodWithMessageHandledByProjectionSetToTrue()
        {
            var projectionMock = new Mock<TestProjection>(null) { CallBase = true };
            projectionMock.Setup(x => x.MockIncrementSequenceNumber(It.IsAny<bool>()));
            var registeredMessageEnvelope = new FakeMessageEnvelope(1, new RegisteredMessageA());

            IProjection<FakeMessageEnvelope> projection = projectionMock.Object;
            projection.Handle(() => new FakeConnection(), registeredMessageEnvelope, CancellationToken.None);

            projectionMock.Verify(x => x.MockIncrementSequenceNumber(true), Times.Once);
            projectionMock.Verify(x => x.MockIncrementSequenceNumber(false), Times.Never);
        }

        [Test]
        public void Handle_MessageWithoutRegisteredHandler_ShouldCallIncrementNextSequenceNumberMethodWithMessageHandledByProjectionSetToFalse()
        {
            var projectionMock = new Mock<TestProjection>(null) { CallBase = true };
            projectionMock.Setup(x => x.MockIncrementSequenceNumber(It.IsAny<bool>()));
            var unregisteredMessageEnvelope = new FakeMessageEnvelope(1, new UnregisteredMessage());

            IProjection<FakeMessageEnvelope> projection = projectionMock.Object;
            projection.Handle(() => new FakeConnection(), unregisteredMessageEnvelope, CancellationToken.None);

            projectionMock.Verify(x => x.MockIncrementSequenceNumber(true), Times.Never);
            projectionMock.Verify(x => x.MockIncrementSequenceNumber(false), Times.Once);
        }
    }
}
