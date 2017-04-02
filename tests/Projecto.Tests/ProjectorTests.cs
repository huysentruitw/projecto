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
        private ProjectScopeFactory<FakeMessageEnvelope> _projectScopeFactory;
        private Mock<IProjection<FakeMessageEnvelope>>[] _projectionMocks;

        [SetUp]
        public void SetUp()
        {
            _projectScopeFactory = _ => new FakeProjectScope();
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
            var ex = Assert.Throws<ArgumentNullException>(() => new Projector<FakeMessageEnvelope>(null, _projectScopeFactory));
            Assert.That(ex.ParamName, Is.EqualTo("projections"));
        }

        [Test]
        public void Constructor_PassEmptyProjectionSet_ShouldThrowException()
        {
            var emptySet = new HashSet<IProjection<FakeMessageEnvelope>>();
            var ex = Assert.Throws<ArgumentException>(() => new Projector<FakeMessageEnvelope>(emptySet, _projectScopeFactory));
            Assert.That(ex.ParamName, Is.EqualTo("projections"));
        }

        [Test]
        public void Constructor_PassNullAsProjectScopeFactory_ShouldThrowException()
        {
            var projections = new HashSet<IProjection<FakeMessageEnvelope>>(_projectionMocks.Select(x => x.Object));
            var ex = Assert.Throws<ArgumentNullException>(() => new Projector<FakeMessageEnvelope>(projections, null));
            Assert.That(ex.ParamName, Is.EqualTo("projectScopeFactory"));
        }

        [Test]
        public void NextSequence_GetAfterConstruction_ShouldReturnLowestNextSequence()
        {
            _projectionMocks[0].SetupGet(x => x.NextSequenceNumber).Returns(100);
            _projectionMocks[1].SetupGet(x => x.NextSequenceNumber).Returns(73);
            _projectionMocks[2].SetupGet(x => x.NextSequenceNumber).Returns(102);

            var projections = new HashSet<IProjection<FakeMessageEnvelope>>(_projectionMocks.Select(x => x.Object));
            var projector = new Projector<FakeMessageEnvelope>(projections, _projectScopeFactory);
            Assert.That(projector.NextSequenceNumber, Is.EqualTo(73));
        }

        [Test]
        public void Project_MessageWithWrongSequenceNumber_ShouldThrowException()
        {
            var messageEnvelope = new FakeMessageEnvelope(new MessageA());
            _projectionMocks[0].SetupGet(x => x.NextSequenceNumber).Returns(5);
            _projectionMocks[1].SetupGet(x => x.NextSequenceNumber).Returns(6);
            _projectionMocks[2].SetupGet(x => x.NextSequenceNumber).Returns(3);

            var projections = new HashSet<IProjection<FakeMessageEnvelope>>(_projectionMocks.Select(x => x.Object));
            var projector = new Projector<FakeMessageEnvelope>(projections, _projectScopeFactory);
            var ex = Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => projector.Project(2, messageEnvelope));
            Assert.That(ex.ParamName, Is.EqualTo("sequenceNumber"));
        }

        [Test]
        public void Project_ProjectionDoesntIncrementSequenceNumber_ShouldThrowException()
        {
            var messageEnvelope = new FakeMessageEnvelope(new MessageA());
            _projectionMocks[0].SetupGet(x => x.NextSequenceNumber).Returns(5);
            _projectionMocks[1].SetupGet(x => x.NextSequenceNumber).Returns(6);
            _projectionMocks[2].SetupGet(x => x.NextSequenceNumber).Returns(3);

            var projections = new HashSet<IProjection<FakeMessageEnvelope>>(_projectionMocks.Select(x => x.Object));
            var projector = new Projector<FakeMessageEnvelope>(projections, _projectScopeFactory);
            var ex = Assert.ThrowsAsync<InvalidOperationException>(() => projector.Project(3, messageEnvelope));
            Assert.That(ex.Message, Contains.Substring("did not increment NextSequence (3) after processing event"));
        }

        [Test]
        public async Task Project_MessageWithCorrectSequenceNumber_ShouldIncrementSequenceNumber()
        {
            var messageEnvelope = new FakeMessageEnvelope(new MessageA());
            var nextSequence = 3;
            _projectionMocks[0].SetupGet(x => x.NextSequenceNumber).Returns(5);
            _projectionMocks[1].SetupGet(x => x.NextSequenceNumber).Returns(6);
            _projectionMocks[2].SetupGet(x => x.NextSequenceNumber).Returns(() => nextSequence);
            _projectionMocks[2]
                .Setup(x => x.Handle(It.IsAny<Func<Type, object>>(), messageEnvelope, CancellationToken.None))
                .Callback(() => nextSequence++)
                .Returns(() => Task.FromResult(0));

            var projections = new HashSet<IProjection<FakeMessageEnvelope>>(_projectionMocks.Select(x => x.Object));
            var projector = new Projector<FakeMessageEnvelope>(projections, _projectScopeFactory);
            Assert.That(projector.NextSequenceNumber, Is.EqualTo(3));
            await projector.Project(3, messageEnvelope);
            Assert.That(projector.NextSequenceNumber, Is.EqualTo(4));
        }

        [Test]
        public async Task Project_MessageWithCorrectSequenceNumber_ShouldCallHandleMethodOfProjectionsWithMatchingSequenceNumber()
        {
            var messageEnvelope = new FakeMessageEnvelope(new MessageA());
            var nextSequences = new[] {5, 6, 5};
            _projectionMocks[0].SetupGet(x => x.NextSequenceNumber).Returns(() => nextSequences[0]);
            _projectionMocks[0]
                .Setup(x => x.Handle(It.IsAny<Func<Type, object>>(), messageEnvelope, CancellationToken.None))
                .Callback(() => nextSequences[0]++).Returns(() => Task.FromResult(0));

            _projectionMocks[1].SetupGet(x => x.NextSequenceNumber).Returns(() => nextSequences[1]);
            _projectionMocks[1]
                .Setup(x => x.Handle(It.IsAny<Func<Type, object>>(), messageEnvelope, CancellationToken.None))
                .Callback(() => nextSequences[1]++).Returns(() => Task.FromResult(0));

            _projectionMocks[2].SetupGet(x => x.NextSequenceNumber).Returns(() => nextSequences[2]);
            _projectionMocks[2]
                .Setup(x => x.Handle(It.IsAny<Func<Type, object>>(), messageEnvelope, CancellationToken.None))
                .Callback(() => nextSequences[2]++).Returns(() => Task.FromResult(0));

            var projections = new HashSet<IProjection<FakeMessageEnvelope>>(_projectionMocks.Select(x => x.Object));
            var projector = new Projector<FakeMessageEnvelope>(projections, _projectScopeFactory);

            await projector.Project(5, messageEnvelope);
            _projectionMocks[0].Verify(
                x => x.Handle(It.IsAny<Func<Type, object>>(), It.IsAny<FakeMessageEnvelope>(), It.IsAny<CancellationToken>()),
                Times.Once);
            _projectionMocks[1].Verify(
                x => x.Handle(It.IsAny<Func<Type, object>>(), It.IsAny<FakeMessageEnvelope>(), It.IsAny<CancellationToken>()),
                Times.Never);
            _projectionMocks[2].Verify(
                x => x.Handle(It.IsAny<Func<Type, object>>(), It.IsAny<FakeMessageEnvelope>(), It.IsAny<CancellationToken>()),
                Times.Once);

            await projector.Project(6, messageEnvelope);
            _projectionMocks[0].Verify(
                x => x.Handle(It.IsAny<Func<Type, object>>(), It.IsAny<FakeMessageEnvelope>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));
            _projectionMocks[1].Verify(
                x => x.Handle(It.IsAny<Func<Type, object>>(), It.IsAny<FakeMessageEnvelope>(), It.IsAny<CancellationToken>()),
                Times.Once);
            _projectionMocks[2].Verify(
                x => x.Handle(It.IsAny<Func<Type, object>>(), It.IsAny<FakeMessageEnvelope>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }

        [Test]
        public async Task Project_CancelMessageWithCorrectSequenceNumber_ShouldCancelHandlerAndNotThrowSequenceNumberException()
        {
            var messageEnvelope = new FakeMessageEnvelope(new MessageA());
            var isCancelled = false;
            _projectionMocks[0].SetupGet(x => x.NextSequenceNumber).Returns(() => 10);
            _projectionMocks[0]
                .Setup(x => x.Handle(It.IsAny<Func<Type, object>>(), messageEnvelope, It.IsAny<CancellationToken>()))
                .Returns<object, FakeMessageEnvelope, CancellationToken>((_, __, token) =>
                {
                    return Task.Run(() =>
                    {
                        Thread.Sleep(20);
                        isCancelled = token.IsCancellationRequested;
                    });
                });

            var projections = new HashSet<IProjection<FakeMessageEnvelope>>(_projectionMocks.Take(1).Select(x => x.Object));
            var projector = new Projector<FakeMessageEnvelope>(projections, _projectScopeFactory);
            var cancellationTokenSource = new CancellationTokenSource(10);
            await projector.Project(10, messageEnvelope, cancellationTokenSource.Token);
            Assert.True(isCancelled);
        }

        [Test]
        public async Task Project_WithProjectionThatResolvesConnection_ShouldCreateResolveDisposeProjectScopeInCorrectOrder()
        {
            var messageEnvelope = new FakeMessageEnvelope(new MessageA());
            var nextSequence = 5;
            var connectionType = typeof(ConnectionA);

            _projectionMocks[0].SetupGet(x => x.NextSequenceNumber).Returns(() => nextSequence);
            _projectionMocks[0]
                .Setup(x => x.Handle(It.IsAny<Func<Type, object>>(), messageEnvelope, CancellationToken.None))
                .Callback<Func<Type, object>, FakeMessageEnvelope, CancellationToken>((connectionResolver, _, __) =>
                {
                    connectionResolver(connectionType);
                    nextSequence++;
                })
                .Returns(() => Task.FromResult(0));

            var executionOrder = 0;
            var scopeMock = new Mock<ProjectScope>(null);
            scopeMock.Setup(x => x.ResolveConnection(connectionType)).Callback(() => Assert.That(executionOrder++, Is.EqualTo(1)));
            scopeMock.Setup(x => x.BeforeDispose()).Callback(() => Assert.That(executionOrder++, Is.EqualTo(2))).Returns(() => Task.FromResult(0));
            scopeMock.Setup(x => x.Dispose()).Callback(() => Assert.That(executionOrder++, Is.EqualTo(3)));

            var projections = new HashSet<IProjection<FakeMessageEnvelope>>(_projectionMocks.Take(1).Select(x => x.Object));
            var projector = new Projector<FakeMessageEnvelope>(projections, _ =>
            {
                Assert.That(executionOrder++, Is.EqualTo(0));
                return scopeMock.Object;
            });

            await projector.Project(nextSequence, messageEnvelope);

            Assert.That(executionOrder, Is.EqualTo(4));
        }
    }
}
