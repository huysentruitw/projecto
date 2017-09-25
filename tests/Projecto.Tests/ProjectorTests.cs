using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Projecto.DependencyInjection;
using Projecto.Tests.TestClasses;

namespace Projecto.Tests
{
    [TestFixture]
    [SuppressMessage("ReSharper", "ObjectCreationAsStatement")]
    public class ProjectorTests
    {
        private readonly Mock<IConnectionLifetimeScopeFactory> _factoryMock;
        private Mock<IProjection<FakeMessageEnvelope>>[] _projectionMocks;

        public ProjectorTests()
        {
            _factoryMock = new Mock<IConnectionLifetimeScopeFactory>();
        }

        [SetUp]
        public void SetUp()
        {
            _factoryMock.Reset();
            _factoryMock.Setup(x => x.BeginLifetimeScope()).Returns(() => new Mock<IConnectionLifetimeScope>().Object);

            _projectionMocks = new []
            {
                new Mock<IProjection<FakeMessageEnvelope>>(),
                new Mock<IProjection<FakeMessageEnvelope>>(),
                new Mock<IProjection<FakeMessageEnvelope>>()
            };
        }

        [Test]
        public void Constructor_PassNullAsProjectionSet_ShouldThrowException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => new Projector<FakeMessageEnvelope>(null, _factoryMock.Object));
            Assert.That(ex.ParamName, Is.EqualTo("projections"));
        }

        [Test]
        public void Constructor_PassEmptyProjectionSet_ShouldThrowException()
        {
            var emptySet = new HashSet<IProjection<FakeMessageEnvelope>>();
            var ex = Assert.Throws<ArgumentException>(() => new Projector<FakeMessageEnvelope>(emptySet, _factoryMock.Object));
            Assert.That(ex.ParamName, Is.EqualTo("projections"));
        }

        [Test]
        public void Constructor_PassNullAsConnectionLifetimeScopeFactory_ShouldThrowException()
        {
            var projections = new HashSet<IProjection<FakeMessageEnvelope>>(_projectionMocks.Select(x => x.Object));
            var ex = Assert.Throws<ArgumentNullException>(() => new Projector<FakeMessageEnvelope>(projections, null));
            Assert.That(ex.ParamName, Is.EqualTo("connectionLifetimeScopeFactory"));
        }

        [Test]
        public void GetNextSequenceNumber_GetAfterConstruction_ShouldReturnLowestNextSequence()
        {
            _projectionMocks[0].Setup(x => x.GetNextSequenceNumber(It.IsAny<IConnectionLifetimeScope>())).Returns(100);
            _projectionMocks[1].Setup(x => x.GetNextSequenceNumber(It.IsAny<IConnectionLifetimeScope>())).Returns(73);
            _projectionMocks[2].Setup(x => x.GetNextSequenceNumber(It.IsAny<IConnectionLifetimeScope>())).Returns(102);

            var projections = new HashSet<IProjection<FakeMessageEnvelope>>(_projectionMocks.Select(x => x.Object));
            var projector = new Projector<FakeMessageEnvelope>(projections, _factoryMock.Object);
            Assert.That(projector.GetNextSequenceNumber(), Is.EqualTo(73));
        }

        [Test]
        public void Project_MessageWithWrongSequenceNumber_ShouldThrowException()
        {
            var messageEnvelope = new FakeMessageEnvelope(2, new RegisteredMessageA());
            _projectionMocks[0].Setup(x => x.GetNextSequenceNumber(It.IsAny<IConnectionLifetimeScope>())).Returns(5);
            _projectionMocks[1].Setup(x => x.GetNextSequenceNumber(It.IsAny<IConnectionLifetimeScope>())).Returns(6);
            _projectionMocks[2].Setup(x => x.GetNextSequenceNumber(It.IsAny<IConnectionLifetimeScope>())).Returns(3);

            var projections = new HashSet<IProjection<FakeMessageEnvelope>>(_projectionMocks.Select(x => x.Object));
            var projector = new Projector<FakeMessageEnvelope>(projections, _factoryMock.Object);
            var ex = Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => projector.Project(messageEnvelope));
            Assert.That(ex.ParamName, Is.EqualTo("SequenceNumber"));
        }

        [Test]
        public void Project_ProjectionDoesntIncrementSequenceNumber_ShouldThrowException()
        {
            var messageEnvelope = new FakeMessageEnvelope(3, new RegisteredMessageA());
            _projectionMocks[0].Setup(x => x.GetNextSequenceNumber(It.IsAny<IConnectionLifetimeScope>())).Returns(5);
            _projectionMocks[1].Setup(x => x.GetNextSequenceNumber(It.IsAny<IConnectionLifetimeScope>())).Returns(6);
            _projectionMocks[2].Setup(x => x.GetNextSequenceNumber(It.IsAny<IConnectionLifetimeScope>())).Returns(3);

            var projections = new HashSet<IProjection<FakeMessageEnvelope>>(_projectionMocks.Select(x => x.Object));
            var projector = new Projector<FakeMessageEnvelope>(projections, _factoryMock.Object);
            var ex = Assert.ThrowsAsync<InvalidOperationException>(() => projector.Project(messageEnvelope));
            Assert.That(ex.Message, Contains.Substring("did not increment NextSequence (3) after processing message"));
        }

        [Test]
        public async Task Project_MessageWithCorrectSequenceNumber_ShouldIncrementSequenceNumber()
        {
            var nextSequence = 3;
            var messageEnvelope = new FakeMessageEnvelope(nextSequence, new RegisteredMessageA());
            _projectionMocks[0].Setup(x => x.GetNextSequenceNumber(It.IsAny<IConnectionLifetimeScope>())).Returns(5);
            _projectionMocks[1].Setup(x => x.GetNextSequenceNumber(It.IsAny<IConnectionLifetimeScope>())).Returns(6);
            _projectionMocks[2].Setup(x => x.GetNextSequenceNumber(It.IsAny<IConnectionLifetimeScope>())).Returns(() => nextSequence);
            _projectionMocks[2]
                .Setup(x => x.Handle(It.IsAny<IConnectionLifetimeScope>(), messageEnvelope, CancellationToken.None))
                .Callback(() => nextSequence++)
                .Returns(() => Task.FromResult(0));

            var projections = new HashSet<IProjection<FakeMessageEnvelope>>(_projectionMocks.Select(x => x.Object));
            var projector = new Projector<FakeMessageEnvelope>(projections, _factoryMock.Object);
            Assert.That(projector.GetNextSequenceNumber(), Is.EqualTo(3));
            await projector.Project(messageEnvelope);
            Assert.That(projector.GetNextSequenceNumber(), Is.EqualTo(4));
        }

        [Test]
        public async Task Project_MessageWithCorrectSequenceNumber_ShouldCallHandleMethodOfProjectionsWithMatchingSequenceNumber()
        {
            var message = new RegisteredMessageA();
            var nextSequences = new[] { 5, 6, 5 };
            _projectionMocks[0].Setup(x => x.GetNextSequenceNumber(It.IsAny<IConnectionLifetimeScope>())).Returns(() => nextSequences[0]);
            _projectionMocks[0]
                .Setup(x => x.Handle(It.IsAny<IConnectionLifetimeScope>(), It.Is<FakeMessageEnvelope>(e => e.Message == message), CancellationToken.None))
                .Callback(() => nextSequences[0]++).Returns(() => Task.FromResult(0));

            _projectionMocks[1].Setup(x => x.GetNextSequenceNumber(It.IsAny<IConnectionLifetimeScope>())).Returns(() => nextSequences[1]);
            _projectionMocks[1]
                .Setup(x => x.Handle(It.IsAny<IConnectionLifetimeScope>(), It.Is<FakeMessageEnvelope>(e => e.Message == message), CancellationToken.None))
                .Callback(() => nextSequences[1]++).Returns(() => Task.FromResult(0));

            _projectionMocks[2].Setup(x => x.GetNextSequenceNumber(It.IsAny<IConnectionLifetimeScope>())).Returns(() => nextSequences[2]);
            _projectionMocks[2]
                .Setup(x => x.Handle(It.IsAny<IConnectionLifetimeScope>(), It.Is<FakeMessageEnvelope>(e => e.Message == message), CancellationToken.None))
                .Callback(() => nextSequences[2]++).Returns(() => Task.FromResult(0));

            var projections = new HashSet<IProjection<FakeMessageEnvelope>>(_projectionMocks.Select(x => x.Object));
            var projector = new Projector<FakeMessageEnvelope>(projections, _factoryMock.Object);

            await projector.Project(new FakeMessageEnvelope(5, message));
            _projectionMocks[0].Verify(
                x => x.Handle(It.IsAny<IConnectionLifetimeScope>(), It.IsAny<FakeMessageEnvelope>(), It.IsAny<CancellationToken>()),
                Times.Once);
            _projectionMocks[1].Verify(
                x => x.Handle(It.IsAny<IConnectionLifetimeScope>(), It.IsAny<FakeMessageEnvelope>(), It.IsAny<CancellationToken>()),
                Times.Never);
            _projectionMocks[2].Verify(
                x => x.Handle(It.IsAny<IConnectionLifetimeScope>(), It.IsAny<FakeMessageEnvelope>(), It.IsAny<CancellationToken>()),
                Times.Once);

            await projector.Project(new FakeMessageEnvelope(6, message));
            _projectionMocks[0].Verify(
                x => x.Handle(It.IsAny<IConnectionLifetimeScope>(), It.IsAny<FakeMessageEnvelope>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));
            _projectionMocks[1].Verify(
                x => x.Handle(It.IsAny<IConnectionLifetimeScope>(), It.IsAny<FakeMessageEnvelope>(), It.IsAny<CancellationToken>()),
                Times.Once);
            _projectionMocks[2].Verify(
                x => x.Handle(It.IsAny<IConnectionLifetimeScope>(), It.IsAny<FakeMessageEnvelope>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }

        [Test]
        public async Task Project_CancelMessageWithCorrectSequenceNumber_ShouldCancelHandlerAndNotThrowSequenceNumberException()
        {
            var sequenceNumber = 10;
            var messageEnvelope = new FakeMessageEnvelope(sequenceNumber, new RegisteredMessageA());
            var isCancelled = false;
            _projectionMocks[0].Setup(x => x.GetNextSequenceNumber(It.IsAny<IConnectionLifetimeScope>())).Returns(() => sequenceNumber);
            _projectionMocks[0]
                .Setup(x => x.Handle(It.IsAny<IConnectionLifetimeScope>(), messageEnvelope, It.IsAny<CancellationToken>()))
                .Returns<object, FakeMessageEnvelope, CancellationToken>((_, __, token) =>
                {
                    // ReSharper disable once MethodSupportsCancellation
                    return Task.Run(() =>
                    {
                        Thread.Sleep(20);
                        isCancelled = token.IsCancellationRequested;
                    });
                });

            var projections = new HashSet<IProjection<FakeMessageEnvelope>>(_projectionMocks.Take(1).Select(x => x.Object));
            var projector = new Projector<FakeMessageEnvelope>(projections, _factoryMock.Object);
            var cancellationTokenSource = new CancellationTokenSource(10);
            await projector.Project(messageEnvelope, cancellationTokenSource.Token);
            Assert.True(isCancelled);
        }

        [Test]
        public async Task Project_WithProjectionThatResolvesConnection_ShouldCreateResolveDisposeConnectionLifetimeScopeInCorrectOrder()
        {
            var nextSequence = 5;
            var messageEnvelope = new FakeMessageEnvelope(nextSequence, new RegisteredMessageA());

            _projectionMocks[0].Setup(x => x.GetNextSequenceNumber(It.IsAny<IConnectionLifetimeScope>())).Returns(() => nextSequence);
            _projectionMocks[0]
                .Setup(x => x.Handle(It.IsAny<IConnectionLifetimeScope>(), messageEnvelope, CancellationToken.None))
                .Callback<IConnectionLifetimeScope, FakeMessageEnvelope, CancellationToken>((scope, _, __) =>
                {
                    Assert.That(scope.ResolveConnection<ConnectionA>(), Is.Not.Null);
                    nextSequence++;
                })
                .Returns(() => Task.FromResult(0));

            var executionOrder = 0;

            var projections = new HashSet<IProjection<FakeMessageEnvelope>>(_projectionMocks.Take(1).Select(x => x.Object));
            var projector = new Projector<FakeMessageEnvelope>(projections, _factoryMock.Object);

            // Get the sequence number first so this scope creation/disposal does not influence our test
            projector.GetNextSequenceNumber();

            _factoryMock
                .Setup(x => x.BeginLifetimeScope())
                .Callback(() => Assert.That(executionOrder++, Is.EqualTo(0)))
                .Returns(() =>
                {
                    var scopeMock = new Mock<IConnectionLifetimeScope>();
                    scopeMock
                        .Setup(x => x.ResolveConnection<ConnectionA>())
                        .Callback(() => Assert.That(executionOrder++, Is.EqualTo(1)))
                        .Returns(() => new ConnectionA());

                    scopeMock
                        .Setup(x => x.Dispose())
                        .Callback(() => Assert.That(executionOrder++, Is.EqualTo(2)));
                    return scopeMock.Object;
                });

            // ReSharper disable once MethodSupportsCancellation
            await projector.Project(messageEnvelope);

            Assert.That(executionOrder, Is.EqualTo(3));
        }

        [Test]
        public async Task Project_WithProjectionThatNeedsSeparateHandleAndSequenceNumberConnection_ShouldResolveBothConnectionInCorrectOrder()
        {
            var nextSequence = 7;
            var messageEnvelope = new FakeMessageEnvelope(nextSequence, new RegisteredMessageB());

            _projectionMocks[0]
                .Setup(x => x.GetNextSequenceNumber(It.IsAny<IConnectionLifetimeScope>()))
                .Callback<IConnectionLifetimeScope>(scope => scope.ResolveConnection<ConnectionA>())
                .Returns(() => nextSequence);
            _projectionMocks[0]
                .Setup(x => x.Handle(It.IsAny<IConnectionLifetimeScope>(), messageEnvelope, CancellationToken.None))
                .Callback<IConnectionLifetimeScope, FakeMessageEnvelope, CancellationToken>((connectionResolver, _, __) =>
                {
                    Assert.That(connectionResolver.ResolveConnection<ConnectionB>(), Is.Not.Null);
                    nextSequence++;
                })
                .Returns(() => Task.FromResult(0));

            var projections = new HashSet<IProjection<FakeMessageEnvelope>>(_projectionMocks.Take(1).Select(x => x.Object));
            var projector = new Projector<FakeMessageEnvelope>(projections, _factoryMock.Object);

            var executionOrder = 0;

            _factoryMock
                .Setup(x => x.BeginLifetimeScope())
                .Callback(() => Assert.That(executionOrder++, Is.EqualTo(0)))
                .Returns(() =>
                {
                    var scopeMock = new Mock<IConnectionLifetimeScope>();
                    scopeMock
                        .Setup(x => x.ResolveConnection<ConnectionA>())
                        .Callback(() => Assert.That(executionOrder++, Is.EqualTo(1).Or.EqualTo(2).Or.EqualTo(4)))
                        .Returns(() => new ConnectionA());

                    scopeMock
                        .Setup(x => x.ResolveConnection<ConnectionB>())
                        .Callback(() => Assert.That(executionOrder++, Is.EqualTo(3)))
                        .Returns(() => new ConnectionB());
                    return scopeMock.Object;
                });

            // ReSharper disable once MethodSupportsCancellation
            await projector.Project(messageEnvelope);

            Assert.That(executionOrder, Is.EqualTo(5));
        }

        [Test]
        public void GetNextSequenceNumber_WithProjectionThatResolvesConnection_ShouldCreateResolveDisposeConnectionLifetimeScopeInCorrectOrder()
        {
            var nextSequence = 5;
            var messageEnvelope = new FakeMessageEnvelope(nextSequence, new RegisteredMessageA());

            _projectionMocks[0]
                .Setup(x => x.GetNextSequenceNumber(It.IsAny<IConnectionLifetimeScope>()))
                .Callback<IConnectionLifetimeScope>(scope => scope.ResolveConnection<ConnectionA>())
                .Returns(() => nextSequence);
            _projectionMocks[0]
                .Setup(x => x.Handle(It.IsAny<IConnectionLifetimeScope>(), messageEnvelope, CancellationToken.None))
                .Callback<IConnectionLifetimeScope, FakeMessageEnvelope, CancellationToken>((connectionResolver, _, __) =>
                {
                    Assert.That(connectionResolver.ResolveConnection<ConnectionA>(), Is.Not.Null);
                    nextSequence++;
                })
                .Returns(() => Task.FromResult(0));

            var executionOrder = 0;

            _factoryMock
                .Setup(x => x.BeginLifetimeScope())
                .Callback(() => Assert.That(executionOrder++, Is.EqualTo(0)))
                .Returns(() =>
                {
                    var scopeMock = new Mock<IConnectionLifetimeScope>();
                    scopeMock
                        .Setup(x => x.ResolveConnection<ConnectionA>())
                        .Callback(() => Assert.That(executionOrder++, Is.EqualTo(1)))
                        .Returns(() => new ConnectionA());

                    scopeMock
                        .Setup(x => x.Dispose())
                        .Callback(() => Assert.That(executionOrder++, Is.EqualTo(2)));
                    return scopeMock.Object;
                });

            var projections = new HashSet<IProjection<FakeMessageEnvelope>>(_projectionMocks.Take(1).Select(x => x.Object));
            var projector = new Projector<FakeMessageEnvelope>(projections, _factoryMock.Object);

            projector.GetNextSequenceNumber();

            Assert.That(executionOrder, Is.EqualTo(3));
        }
    }
}
