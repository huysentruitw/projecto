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
        public void Handle_TwoDifferentMessages_ShouldCallCorrectHandlerWithCorrectArguments()
        {
            var connectionMock = new Mock<FakeConnection>();
            Func<object> connectionFactory = () => connectionMock.Object;
            var token = new CancellationToken();
            var messageA = new RegisteredMessageA();
            var messageB = new RegisteredMessageB();
            var messageEnvelopeA = new FakeMessageEnvelope(1, messageA);
            var messageEnvelopeB = new FakeMessageEnvelope(2, messageB);

            IProjection<string, FakeMessageEnvelope> projection = new TestProjection("A");
            projection.Handle(connectionFactory, messageEnvelopeA, token);
            projection.Handle(connectionFactory, messageEnvelopeB, token);

            connectionMock.Verify(x => x.UpdateA(messageEnvelopeA, messageA), Times.Once);
            connectionMock.Verify(x => x.UpdateB(messageEnvelopeB, messageB, token), Times.Once);
        }

        [Test]
        public async Task Handle_MessageWithRegisteredHandler_ShouldReturnTrue()
        {
            var projectionMock = new Mock<TestProjection>(null) { CallBase = true };
            var registeredMessageEnvelope = new FakeMessageEnvelope(1, new RegisteredMessageA());

            IProjection<string, FakeMessageEnvelope> projection = projectionMock.Object;
            Assert.That(await projection.Handle(_connectionFactory, registeredMessageEnvelope, CancellationToken.None), Is.True);
        }

        [Test]
        public async Task Handle_MessageWithoutRegisteredHandler_ShouldReturnFalse()
        {
            var projectionMock = new Mock<TestProjection>(null) { CallBase = true };
            var unregisteredMessageEnvelope = new FakeMessageEnvelope(1, new UnregisteredMessage());

            IProjection<string, FakeMessageEnvelope> projection = projectionMock.Object;
            Assert.That(await projection.Handle(_connectionFactory, unregisteredMessageEnvelope, CancellationToken.None), Is.False);
        }
    }
}
