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
        private readonly Mock<IDependencyLifetimeScopeFactory> _factoryMock = new Mock<IDependencyLifetimeScopeFactory>();
        private Mock<IProjection<string, FakeMessageEnvelope>>[] _projectionMocks;
        private TestNextSequenceNumberRepository _sequenceNumberRepository;

        [SetUp]
        public void SetUp()
        {
            _sequenceNumberRepository = new TestNextSequenceNumberRepository();

            _factoryMock.Reset();
            _factoryMock.Setup(x => x.BeginLifetimeScope()).Returns(() =>
            {
                var scopeMock = new Mock<IDependencyLifetimeScope>();

                scopeMock
                    .Setup(x => x.Resolve(typeof(TestNextSequenceNumberRepository)))
                    .Returns(_sequenceNumberRepository);

                return scopeMock.Object;
            });

            _projectionMocks = new []
            {
                new Mock<IProjection<string, FakeMessageEnvelope>>(),
                new Mock<IProjection<string, FakeMessageEnvelope>>(),
                new Mock<IProjection<string, FakeMessageEnvelope>>()
            };
        }

        [Test]
        public void Constructor_PassNullAsProjectionSet_ShouldThrowException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => new Projector<string, FakeMessageEnvelope, TestNextSequenceNumberRepository>(null, _factoryMock.Object));
            Assert.That(ex.ParamName, Is.EqualTo("projections"));
        }

        [Test]
        public void Constructor_PassEmptyProjectionSet_ShouldThrowException()
        {
            var emptySet = new HashSet<IProjection<string, FakeMessageEnvelope>>();
            var ex = Assert.Throws<ArgumentException>(() => new Projector<string, FakeMessageEnvelope, TestNextSequenceNumberRepository>(emptySet, _factoryMock.Object));
            Assert.That(ex.ParamName, Is.EqualTo("projections"));
        }

        [Test]
        public void Constructor_PassNullAsDependencyLifetimeScopeFactory_ShouldThrowException()
        {
            var projections = new HashSet<IProjection<string, FakeMessageEnvelope>>(_projectionMocks.Select(x => x.Object));
            var ex = Assert.Throws<ArgumentNullException>(() => new Projector<string, FakeMessageEnvelope, TestNextSequenceNumberRepository>(projections, null));
            Assert.That(ex.ParamName, Is.EqualTo("dependencyLifetimeScopeFactory"));
        }

        [Test]
        public void Constructor_PassTwoProjectionsWithSameKey_ShouldThrowException()
        {
            var projections = new HashSet<IProjection<string, FakeMessageEnvelope>> { new TestProjection("SomeKey"), new TestProjection("SomeKey") };
            var ex = Assert.Throws<InvalidOperationException>(() => new Projector<string, FakeMessageEnvelope, TestNextSequenceNumberRepository>(projections, _factoryMock.Object));
            Assert.That(ex.Message, Is.EqualTo("One or more projections use the same key (SomeKey)"));
        }

        [Test]
        public async Task GetNextSequenceNumber_GetAfterConstructionWithEmptyRepository_ShouldReturnOne()
        {
            var projections = new HashSet<IProjection<string, FakeMessageEnvelope>> { new TestProjection("A"), new TestProjection("B") };
            var projector = new Projector<string, FakeMessageEnvelope, TestNextSequenceNumberRepository>(projections, _factoryMock.Object);
            Assert.That(await projector.GetNextSequenceNumber(), Is.EqualTo(1));
        }

        [Test]
        public async Task GetNextSequenceNumber_GetAfterConstruction_ShouldReturnLowestNextSequence()
        {
            await _sequenceNumberRepository.Store(new Dictionary<string, int> { { "A", 17 }, { "B", 5 }, { "C", 3 } });
            var projections = new HashSet<IProjection<string, FakeMessageEnvelope>> { new TestProjection("A"), new TestProjection("B") };
            var projector = new Projector<string, FakeMessageEnvelope, TestNextSequenceNumberRepository>(projections, _factoryMock.Object);
            Assert.That(await projector.GetNextSequenceNumber(), Is.EqualTo(5));
        }

        [Test]
        public async Task GetNextSequenceNumber_CallMethodTwice_ShouldNotIncrement()
        {
            var projections = new HashSet<IProjection<string, FakeMessageEnvelope>> { new TestProjection("A"), new TestProjection("B") };
            var projector = new Projector<string, FakeMessageEnvelope, TestNextSequenceNumberRepository>(projections, _factoryMock.Object);
            Assert.That(await projector.GetNextSequenceNumber(), Is.EqualTo(1));
            Assert.That(await projector.GetNextSequenceNumber(), Is.EqualTo(1));
        }

        [Test]
        public async Task GetNextSequenceNumber_GetAfterConstruction_ShouldCreateResolveDisposeLifetimeScopeInCorrectOrder()
        {
            await _sequenceNumberRepository.Store(new Dictionary<string, int> { { "A", 17 }, { "B", 5 }, { "C", 3 } });
            var projections = new HashSet<IProjection<string, FakeMessageEnvelope>> { new TestProjection("A"), new TestProjection("B") };
            var projector = new Projector<string, FakeMessageEnvelope, TestNextSequenceNumberRepository>(projections, _factoryMock.Object);

            var executionOrder = 0;
            _factoryMock.Reset();
            _factoryMock
                .Setup(x => x.BeginLifetimeScope())
                .Callback(() => Assert.That(executionOrder++, Is.EqualTo(0)))
                .Returns(() =>
                {
                    var scopeMock = new Mock<IDependencyLifetimeScope>();

                    scopeMock
                        .Setup(x => x.Resolve(typeof(TestNextSequenceNumberRepository)))
                        .Callback(() => Assert.That(executionOrder++, Is.EqualTo(1)))
                        .Returns(_sequenceNumberRepository);

                    scopeMock
                        .Setup(x => x.Dispose())
                        .Callback(() => Assert.That(executionOrder++, Is.EqualTo(2)));
                    return scopeMock.Object;
                });

            await projector.GetNextSequenceNumber();

            Assert.That(executionOrder, Is.EqualTo(3));
        }

        [Test]
        public async Task GetNextSequenceNumber_CallMethodTwice_ShouldOnlyFetchFromRepositoryOnce()
        {
            await _sequenceNumberRepository.Store(new Dictionary<string, int> { { "A", 17 }, { "B", 5 }, { "C", 3 } });
            var projections = new HashSet<IProjection<string, FakeMessageEnvelope>> { new TestProjection("A"), new TestProjection("B") };
            var projector = new Projector<string, FakeMessageEnvelope, TestNextSequenceNumberRepository>(projections, _factoryMock.Object);

            Assert.That(_sequenceNumberRepository.NumberOfFetchCalls, Is.EqualTo(0));

            await projector.GetNextSequenceNumber();
            await projector.GetNextSequenceNumber();

            Assert.That(_sequenceNumberRepository.NumberOfFetchCalls, Is.EqualTo(1));
        }

        [Test]
        public async Task Project_MessageWithWrongSequenceNumber_ShouldThrowException()
        {
            await _sequenceNumberRepository.Store(new Dictionary<string, int> { { "A", 5 }, { "B", 6 }, { "C", 3 } });
            _projectionMocks[0].SetupGet(x => x.Key).Returns("A");
            _projectionMocks[1].SetupGet(x => x.Key).Returns("B");
            _projectionMocks[2].SetupGet(x => x.Key).Returns("C");

            var messageEnvelope = new FakeMessageEnvelope(2, new RegisteredMessageA());
            var projections = new HashSet<IProjection<string, FakeMessageEnvelope>>(_projectionMocks.Select(x => x.Object));
            var projector = new Projector<string, FakeMessageEnvelope, TestNextSequenceNumberRepository>(projections, _factoryMock.Object);
            var ex = Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => projector.Project(messageEnvelope));
            Assert.That(ex.ParamName, Is.EqualTo("SequenceNumber"));
        }

        [Test]
        public async Task Project_MessageWithCorrectSequenceNumber_ShouldIncrementSequenceNumber()
        {
            await _sequenceNumberRepository.Store(new Dictionary<string, int> { { "A", 5 }, { "B", 6 }, { "C", 3 } });
            _projectionMocks[0].SetupGet(x => x.Key).Returns("A");
            _projectionMocks[1].SetupGet(x => x.Key).Returns("B");
            _projectionMocks[2].SetupGet(x => x.Key).Returns("C");

            var messageEnvelope = new FakeMessageEnvelope(3, new RegisteredMessageA());
            var projections = new HashSet<IProjection<string, FakeMessageEnvelope>>(_projectionMocks.Select(x => x.Object));
            var projector = new Projector<string, FakeMessageEnvelope, TestNextSequenceNumberRepository>(projections, _factoryMock.Object);
            Assert.That(await projector.GetNextSequenceNumber(), Is.EqualTo(3));
            await projector.Project(messageEnvelope);
            Assert.That(await projector.GetNextSequenceNumber(), Is.EqualTo(4));
        }

        [Test]
        public async Task Project_MessageWithCorrectSequenceNumber_ShouldCallHandleMethodOfProjectionsWithMatchingSequenceNumber()
        {
            await _sequenceNumberRepository.Store(new Dictionary<string, int> { { "A", 5 }, { "B", 6 }, { "C", 5 } });
            _projectionMocks[0].SetupGet(x => x.Key).Returns("A");
            _projectionMocks[1].SetupGet(x => x.Key).Returns("B");
            _projectionMocks[2].SetupGet(x => x.Key).Returns("C");

            var message = new RegisteredMessageA();
            _projectionMocks[0]
                .Setup(x => x.Handle(It.IsAny<Func<object>>(), It.Is<FakeMessageEnvelope>(e => e.Message == message), CancellationToken.None))
                .Returns(() => Task.FromResult(true));

            _projectionMocks[1]
                .Setup(x => x.Handle(It.IsAny<Func<object>>(), It.Is<FakeMessageEnvelope>(e => e.Message == message), CancellationToken.None))
                .Returns(() => Task.FromResult(true));

            _projectionMocks[2]
                .Setup(x => x.Handle(It.IsAny<Func<object>>(), It.Is<FakeMessageEnvelope>(e => e.Message == message), CancellationToken.None))
                .Returns(() => Task.FromResult(true));

            var projections = new HashSet<IProjection<string, FakeMessageEnvelope>>(_projectionMocks.Select(x => x.Object));
            var projector = new Projector<string, FakeMessageEnvelope, TestNextSequenceNumberRepository>(projections, _factoryMock.Object);

            await projector.Project(new FakeMessageEnvelope(5, message));
            _projectionMocks[0].Verify(
                x => x.Handle(It.IsAny<Func<object>>(), It.IsAny<FakeMessageEnvelope>(), It.IsAny<CancellationToken>()),
                Times.Once);
            _projectionMocks[1].Verify(
                x => x.Handle(It.IsAny<Func<object>>(), It.IsAny<FakeMessageEnvelope>(), It.IsAny<CancellationToken>()),
                Times.Never);
            _projectionMocks[2].Verify(
                x => x.Handle(It.IsAny<Func<object>>(), It.IsAny<FakeMessageEnvelope>(), It.IsAny<CancellationToken>()),
                Times.Once);

            await projector.Project(new FakeMessageEnvelope(6, message));
            _projectionMocks[0].Verify(
                x => x.Handle(It.IsAny<Func<object>>(), It.IsAny<FakeMessageEnvelope>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));
            _projectionMocks[1].Verify(
                x => x.Handle(It.IsAny<Func<object>>(), It.IsAny<FakeMessageEnvelope>(), It.IsAny<CancellationToken>()),
                Times.Once);
            _projectionMocks[2].Verify(
                x => x.Handle(It.IsAny<Func<object>>(), It.IsAny<FakeMessageEnvelope>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }

        [Test]
        public async Task Project_CancelMessageWithCorrectSequenceNumber_ShouldCancelHandlerAndNotThrowSequenceNumberException()
        {
            await _sequenceNumberRepository.Store(new Dictionary<string, int> { { "A", 10 } });
            var messageEnvelope = new FakeMessageEnvelope(10, new RegisteredMessageA());
            var isCancelled = false;
            _projectionMocks[0].SetupGet(x => x.Key).Returns("A");
            _projectionMocks[0]
                .Setup(x => x.Handle(It.IsAny<Func<object>>(), messageEnvelope, It.IsAny<CancellationToken>()))
                .Returns<object, FakeMessageEnvelope, CancellationToken>(async (_, __, token) =>
                {
                    await Task.Delay(20).ConfigureAwait(false);
                    isCancelled = token.IsCancellationRequested;
                    return false;
                });

            var projections = new HashSet<IProjection<string, FakeMessageEnvelope>>(_projectionMocks.Take(1).Select(x => x.Object));
            var projector = new Projector<string, FakeMessageEnvelope, TestNextSequenceNumberRepository>(projections, _factoryMock.Object);
            var cancellationTokenSource = new CancellationTokenSource(5);
            await projector.Project(messageEnvelope, cancellationTokenSource.Token).ConfigureAwait(false);
            Assert.True(isCancelled);
        }

        [Test]
        public async Task Project_WithProjectionThatResolvesConnection_ShouldCreateResolveDisposeLifetimeScopeInCorrectOrder()
        {
            await _sequenceNumberRepository.Store(new Dictionary<string, int> { { "A", 5 } });
            var messageEnvelope = new FakeMessageEnvelope(5, new RegisteredMessageA());
            var connectionType = typeof(ConnectionA);

            _projectionMocks[0].SetupGet(x => x.Key).Returns("A");
            _projectionMocks[0].SetupGet(x => x.ConnectionType).Returns(connectionType);
            _projectionMocks[0]
                .Setup(x => x.Handle(It.IsAny<Func<object>>(), messageEnvelope, CancellationToken.None))
                .Callback<Func<object>, FakeMessageEnvelope, CancellationToken>((connectionResolver, _, __) =>
                {
                    Assert.That(connectionResolver(), Is.Not.Null);
                })
                .Returns(() => Task.FromResult(true));

            var projections = new HashSet<IProjection<string, FakeMessageEnvelope>>(_projectionMocks.Take(1).Select(x => x.Object));
            var projector = new Projector<string, FakeMessageEnvelope, TestNextSequenceNumberRepository>(projections, _factoryMock.Object);

            // Get the sequence number first so this scope creation/disposal does not influence our test
            await projector.GetNextSequenceNumber();

            var executionOrder = 0;
            _factoryMock.Reset();
            _factoryMock
                .Setup(x => x.BeginLifetimeScope())
                .Callback(() => Assert.That(executionOrder++, Is.EqualTo(0)))
                .Returns(() =>
                {
                    var scopeMock = new Mock<IDependencyLifetimeScope>();

                    scopeMock
                        .Setup(x => x.Resolve(typeof(TestNextSequenceNumberRepository)))
                        .Returns(_sequenceNumberRepository);

                    scopeMock
                        .Setup(x => x.Resolve(connectionType))
                        .Callback(() => Assert.That(executionOrder++, Is.EqualTo(1)))
                        .Returns(() => new FakeConnection());

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
        public async Task Project_SecondProjectionThrowsException_ProjectShouldThrowExceptionAfterStoringCorrectSequenceNumbers()
        {
            await _sequenceNumberRepository.Store(new Dictionary<string, int> { { "A", 50 }, { "B", 50 }, { "C", 50 } });
            _projectionMocks[0].SetupGet(x => x.Key).Returns("A");
            _projectionMocks[1].SetupGet(x => x.Key).Returns("B");
            _projectionMocks[2].SetupGet(x => x.Key).Returns("C");

            var messageEnvelope = new FakeMessageEnvelope(50, new RegisteredMessageA());

            _projectionMocks[1].Setup(x => x.Handle(It.IsAny<Func<object>>(), messageEnvelope, CancellationToken.None))
                .Callback(() => { throw new DivideByZeroException(); });

            var projections = new HashSet<IProjection<string, FakeMessageEnvelope>>(_projectionMocks.Select(x => x.Object));
            var projector = new Projector<string, FakeMessageEnvelope, TestNextSequenceNumberRepository>(projections, _factoryMock.Object);

            Assert.ThrowsAsync<DivideByZeroException>(() => projector.Project(messageEnvelope));

            Assert.That(_sequenceNumberRepository["A"], Is.EqualTo(51));
            Assert.That(_sequenceNumberRepository["B"], Is.EqualTo(50));
            Assert.That(_sequenceNumberRepository["C"], Is.EqualTo(50));
        }

        [Test]
        public async Task Project_TwoProjectionsWithDifferentPriority_ShouldHandleProjectionsInCorrectOrder()
        {
            var projectSequence = string.Empty;

            await _sequenceNumberRepository.Store(new Dictionary<string, int> { { "A", 1 }, { "B", 1 }, { "C", 1 } });
            _projectionMocks[0].SetupGet(x => x.Key).Returns("A");
            _projectionMocks[1].SetupGet(x => x.Key).Returns("B");
            _projectionMocks[2].SetupGet(x => x.Key).Returns("C");

            _projectionMocks[0].Setup(x => x.Handle(It.IsAny<Func<object>>(), It.IsAny<FakeMessageEnvelope>(), CancellationToken.None)).Callback(() => projectSequence += "A").Returns(Task.FromResult(false));
            _projectionMocks[1].Setup(x => x.Handle(It.IsAny<Func<object>>(), It.IsAny<FakeMessageEnvelope>(), CancellationToken.None)).Callback(() => projectSequence += "B").Returns(Task.FromResult(false));
            _projectionMocks[2].Setup(x => x.Handle(It.IsAny<Func<object>>(), It.IsAny<FakeMessageEnvelope>(), CancellationToken.None)).Callback(() => projectSequence += "C").Returns(Task.FromResult(false));

            _projectionMocks[0].SetupGet(x => x.Priority).Returns(Priority.Low);
            _projectionMocks[1].SetupGet(x => x.Priority).Returns(Priority.High);
            _projectionMocks[2].SetupGet(x => x.Priority).Returns(Priority.Normal);

            var projections = new HashSet<IProjection<string, FakeMessageEnvelope>>(_projectionMocks.Select(x => x.Object));
            var projector = new Projector<string, FakeMessageEnvelope, TestNextSequenceNumberRepository>(projections, _factoryMock.Object);
            await projector.Project(new FakeMessageEnvelope(1, new RegisteredMessageA()));

            _projectionMocks[0].SetupGet(x => x.Priority).Returns(Priority.Normal);
            _projectionMocks[1].SetupGet(x => x.Priority).Returns(Priority.Low);
            _projectionMocks[2].SetupGet(x => x.Priority).Returns(Priority.High);

            projections = new HashSet<IProjection<string, FakeMessageEnvelope>>(_projectionMocks.Select(x => x.Object));
            projector = new Projector<string, FakeMessageEnvelope, TestNextSequenceNumberRepository>(projections, _factoryMock.Object);
            await projector.Project(new FakeMessageEnvelope(2, new RegisteredMessageA()));

            Assert.That(projectSequence, Is.EqualTo("BCACAB"));
        }
    }
}
