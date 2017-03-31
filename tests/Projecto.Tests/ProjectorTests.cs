using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Projecto.Infrastructure;
using Projecto.Tests.TestClasses;

namespace Projecto.Tests
{
    [TestFixture]
    public class ProjectorTests
    {
        private Mock<ConnectionResolver<FakeProjectContext>> _connectionResolverMock;
        private Mock<IProjection<FakeProjectContext>>[] _projectionMocks;
        private FakeProjectContext _projectContext;

        [SetUp]
        public void SetUp()
        {
            _connectionResolverMock = new Mock<ConnectionResolver<FakeProjectContext>>();
            _projectionMocks = new []
            {
                new Mock<IProjection<FakeProjectContext>>(),
                new Mock<IProjection<FakeProjectContext>>(),
                new Mock<IProjection<FakeProjectContext>>()
            };
            _projectContext = new FakeProjectContext();
        }

        [Test]
        public void Constructor_PassNullAsProjectionSet_ShouldThrowException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => new Projector<FakeProjectContext>(null, _connectionResolverMock.Object));
            Assert.That(ex.ParamName, Is.EqualTo("projections"));
        }

        [Test]
        public void Constructor_PassEmptyProjectionSet_ShouldThrowException()
        {
            var emptySet = new HashSet<IProjection<FakeProjectContext>>();
            var ex = Assert.Throws<ArgumentException>(() => new Projector<FakeProjectContext>(emptySet, _connectionResolverMock.Object));
            Assert.That(ex.ParamName, Is.EqualTo("projections"));
        }

        [Test]
        public void Constructor_PassNullAsConnectionResolver_ShouldThrowException()
        {
            var projections = new HashSet<IProjection<FakeProjectContext>>(_projectionMocks.Select(x => x.Object));
            var ex = Assert.Throws<ArgumentNullException>(() => new Projector<FakeProjectContext>(projections, null));
            Assert.That(ex.ParamName, Is.EqualTo("connectionResolver"));
        }

        [Test]
        public void NextSequence_GetAfterConstruction_ShouldReturnLowestNextSequence()
        {
            _projectionMocks[0].SetupGet(x => x.NextSequenceNumber).Returns(100);
            _projectionMocks[1].SetupGet(x => x.NextSequenceNumber).Returns(73);
            _projectionMocks[2].SetupGet(x => x.NextSequenceNumber).Returns(102);

            var projections = new HashSet<IProjection<FakeProjectContext>>(_projectionMocks.Select(x => x.Object));
            var projector = new Projector<FakeProjectContext>(projections, _connectionResolverMock.Object);
            Assert.That(projector.NextSequenceNumber, Is.EqualTo(73));
        }

        [Test]
        public void Project_MessageWithWrongSequenceNumber_ShouldThrowException()
        {
            _projectionMocks[0].SetupGet(x => x.NextSequenceNumber).Returns(5);
            _projectionMocks[1].SetupGet(x => x.NextSequenceNumber).Returns(6);
            _projectionMocks[2].SetupGet(x => x.NextSequenceNumber).Returns(3);

            var projections = new HashSet<IProjection<FakeProjectContext>>(_projectionMocks.Select(x => x.Object));
            var projector = new Projector<FakeProjectContext>(projections, _connectionResolverMock.Object);
            var ex = Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => projector.Project(2, _projectContext, new MessageA()));
            Assert.That(ex.ParamName, Is.EqualTo("sequenceNumber"));
        }

        [Test]
        public async Task Project_ProjectionDoesntIncrementSequenceNumber_ShouldThrowException()
        {
            var message = new MessageA();
            _projectionMocks[0].SetupGet(x => x.NextSequenceNumber).Returns(5);
            _projectionMocks[1].SetupGet(x => x.NextSequenceNumber).Returns(6);
            _projectionMocks[2].SetupGet(x => x.NextSequenceNumber).Returns(3);

            var projections = new HashSet<IProjection<FakeProjectContext>>(_projectionMocks.Select(x => x.Object));
            var projector = new Projector<FakeProjectContext>(projections, _connectionResolverMock.Object);
            var ex = Assert.ThrowsAsync<InvalidOperationException>(() => projector.Project(3, _projectContext, message));
            Assert.That(ex.Message, Contains.Substring("did not increment NextSequence (3) after processing event"));
        }

        [Test]
        public async Task Project_MessageWithCorrectSequenceNumber_ShouldIncrementSequenceNumber()
        {
            var message = new MessageA();
            var nextSequence = 3;
            _projectionMocks[0].SetupGet(x => x.NextSequenceNumber).Returns(5);
            _projectionMocks[1].SetupGet(x => x.NextSequenceNumber).Returns(6);
            _projectionMocks[2].SetupGet(x => x.NextSequenceNumber).Returns(() => nextSequence);
            _projectionMocks[2]
                .Setup(x => x.Handle(It.IsAny<object>(), _projectContext, message, CancellationToken.None))
                .Callback(() => nextSequence++)
                .Returns(() => Task.FromResult(0));

            var projections = new HashSet<IProjection<FakeProjectContext>>(_projectionMocks.Select(x => x.Object));
            var projector = new Projector<FakeProjectContext>(projections, _connectionResolverMock.Object);
            Assert.That(projector.NextSequenceNumber, Is.EqualTo(3));
            await projector.Project(3, _projectContext, message);
            Assert.That(projector.NextSequenceNumber, Is.EqualTo(4));
        }

        [Test]
        public async Task Project_MessageWithCorrectSequenceNumber_ShouldCallHandleMethodOfProjectionsWithMatchingSequenceNumber()
        {
            var message = new MessageA();
            var nextSequences = new[] {5, 6, 5};
            _projectionMocks[0].SetupGet(x => x.NextSequenceNumber).Returns(() => nextSequences[0]);
            _projectionMocks[0]
                .Setup(x => x.Handle(It.IsAny<object>(), _projectContext, message, CancellationToken.None))
                .Callback(() => nextSequences[0]++).Returns(() => Task.FromResult(0));

            _projectionMocks[1].SetupGet(x => x.NextSequenceNumber).Returns(() => nextSequences[1]);
            _projectionMocks[1]
                .Setup(x => x.Handle(It.IsAny<object>(), _projectContext, message, CancellationToken.None))
                .Callback(() => nextSequences[1]++).Returns(() => Task.FromResult(0));

            _projectionMocks[2].SetupGet(x => x.NextSequenceNumber).Returns(() => nextSequences[2]);
            _projectionMocks[2]
                .Setup(x => x.Handle(It.IsAny<object>(), _projectContext, message, CancellationToken.None))
                .Callback(() => nextSequences[2]++).Returns(() => Task.FromResult(0));

            var projections = new HashSet<IProjection<FakeProjectContext>>(_projectionMocks.Select(x => x.Object));
            var projector = new Projector<FakeProjectContext>(projections, _connectionResolverMock.Object);

            await projector.Project(5, _projectContext, message);
            _projectionMocks[0].Verify(
                x => x.Handle(It.IsAny<object>(), It.IsAny<FakeProjectContext>(), It.IsAny<object>(), It.IsAny<CancellationToken>()),
                Times.Once);
            _projectionMocks[1].Verify(
                x => x.Handle(It.IsAny<object>(), It.IsAny<FakeProjectContext>(), It.IsAny<object>(), It.IsAny<CancellationToken>()),
                Times.Never);
            _projectionMocks[2].Verify(
                x => x.Handle(It.IsAny<object>(), It.IsAny<FakeProjectContext>(), It.IsAny<object>(), It.IsAny<CancellationToken>()),
                Times.Once);

            await projector.Project(6, _projectContext, message);
            _projectionMocks[0].Verify(
                x => x.Handle(It.IsAny<object>(), It.IsAny<FakeProjectContext>(), It.IsAny<object>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));
            _projectionMocks[1].Verify(
                x => x.Handle(It.IsAny<object>(), It.IsAny<FakeProjectContext>(), It.IsAny<object>(), It.IsAny<CancellationToken>()),
                Times.Once);
            _projectionMocks[2].Verify(
                x => x.Handle(It.IsAny<object>(), It.IsAny<FakeProjectContext>(), It.IsAny<object>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }

        [Test]
        public async Task Project_CancelMessageWithCorrectSequenceNumber_ShouldCancelHandlerAndNotThrowSequenceNumberException()
        {
            var message = new MessageA();
            var isCancelled = false;
            _projectionMocks[0].SetupGet(x => x.NextSequenceNumber).Returns(() => 10);
            _projectionMocks[0]
                .Setup(x => x.Handle(It.IsAny<object>(), _projectContext, message, It.IsAny<CancellationToken>()))
                .Returns<object, FakeProjectContext, object, CancellationToken>((conn, ctx, msg, token) =>
                {
                    return Task.Run(() =>
                    {
                        Thread.Sleep(20);
                        isCancelled = token.IsCancellationRequested;
                    });
                });

            var projections = new HashSet<IProjection<FakeProjectContext>>(_projectionMocks.Take(1).Select(x => x.Object));
            var projector = new Projector<FakeProjectContext>(projections, _connectionResolverMock.Object);
            var cancellationTokenSource = new CancellationTokenSource(10);
            await projector.Project(10, _projectContext, message, cancellationTokenSource.Token);
            Assert.True(isCancelled);
        }

        [Test]
        public async Task Project_ProjectionsWithDifferentConnectionType_ShouldPassCorrectConnection()
        {
            var message = new MessageA();
            var nextSequences = new[] { 5, 5, 5 };
            var connectionA = new ConnectionA();
            var connectionB = new ConnectionB();

            _projectionMocks[0].SetupGet(x => x.NextSequenceNumber).Returns(() => nextSequences[0]);
            _projectionMocks[0]
                .Setup(x => x.Handle(connectionA, _projectContext, message, CancellationToken.None))
                .Callback(() => nextSequences[0]++).Returns(() => Task.FromResult(0));
            _projectionMocks[0].SetupGet(x => x.ConnectionType).Returns(typeof(ConnectionA));

            _projectionMocks[1].SetupGet(x => x.NextSequenceNumber).Returns(() => nextSequences[1]);
            _projectionMocks[1]
                .Setup(x => x.Handle(connectionB, _projectContext, message, CancellationToken.None))
                .Callback(() => nextSequences[1]++).Returns(() => Task.FromResult(0));
            _projectionMocks[1].SetupGet(x => x.ConnectionType).Returns(typeof(ConnectionB));

            _projectionMocks[2].SetupGet(x => x.NextSequenceNumber).Returns(() => nextSequences[2]);
            _projectionMocks[2]
                .Setup(x => x.Handle(connectionA, _projectContext, message, CancellationToken.None))
                .Callback(() => nextSequences[2]++).Returns(() => Task.FromResult(0));
            _projectionMocks[2].SetupGet(x => x.ConnectionType).Returns(typeof(ConnectionA));

            _connectionResolverMock.Setup(x => x(_projectContext, typeof(ConnectionA))).Returns(connectionA);
            _connectionResolverMock.Setup(x => x(_projectContext, typeof(ConnectionB))).Returns(connectionB);

            var projections = new HashSet<IProjection<FakeProjectContext>>(_projectionMocks.Select(x => x.Object));
            var projector = new Projector<FakeProjectContext>(projections, _connectionResolverMock.Object);

            await projector.Project(5, _projectContext, message);

            _connectionResolverMock.Verify(x => x(_projectContext, typeof(ConnectionA)), Times.Exactly(2));
            _connectionResolverMock.Verify(x => x(_projectContext, typeof(ConnectionB)), Times.Once);
        }
    }
}
