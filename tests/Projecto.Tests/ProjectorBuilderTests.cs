using System;
using Moq;
using NUnit.Framework;
using Projecto.DependencyInjection;
using Projecto.Tests.TestClasses;

namespace Projecto.Tests
{
    [TestFixture]
    public class ProjectorBuilderTests
    {
        private readonly Mock<IDependencyLifetimeScopeFactory> _factoryMock;

        public ProjectorBuilderTests()
        {
            _factoryMock = new Mock<IDependencyLifetimeScopeFactory>();
            _factoryMock.Setup(x => x.BeginLifetimeScope()).Returns(() => new Mock<IDependencyLifetimeScope>().Object);
        }

        [Test]
        public void Register_PassingNullAsProjection_ShouldThrowException()
        {
            var builder = new ProjectorBuilder<string, FakeMessageEnvelope>();
            var ex = Assert.Throws<ArgumentNullException>(() => builder.Register((IProjection<string, FakeMessageEnvelope>)null));
            Assert.That(ex.ParamName, Is.EqualTo("projection"));
        }

        [Test]
        public void Register_PassingNullAsMultipleProjections_ShouldThrowException()
        {
            var builder = new ProjectorBuilder<string, FakeMessageEnvelope>();
            var ex = Assert.Throws<ArgumentNullException>(() => builder.Register((Projection<string, FakeConnection, FakeMessageEnvelope>[])null));
            Assert.That(ex.ParamName, Is.EqualTo("projections"));
        }

        [Test]
        public void SetDependencyLifetimeScopeFactory_PassingNullAsDependencyLifetimeScopeFactory_ShouldThrowException()
        {
            var builder = new ProjectorBuilder<string, FakeMessageEnvelope>();
            var ex = Assert.Throws<ArgumentNullException>(() => builder.SetDependencyLifetimeScopeFactory(null));
            Assert.That(ex.ParamName, Is.EqualTo("factory"));
        }

        [Test]
        public void Build_WithSingleProjectionRegistered_ShouldGetPassedToProjectorInstance()
        {
            var projection = new TestProjection("A");
            var builder = new ProjectorBuilder<string, FakeMessageEnvelope>();
            builder.Register(projection).SetDependencyLifetimeScopeFactory(_factoryMock.Object);

            var projector = builder.Build<TestNextSequenceNumberRepository>();
            Assert.That(projector.Projections.Length, Is.EqualTo(1));
            Assert.That(projector.Projections[0], Is.EqualTo(projection));
        }

        [Test]
        public void Build_WithMultipleProjectionsRegistered_ShouldGetPassedToProjectorInstance()
        {
            var projections = new[] {new TestProjection("A"), new TestProjection("B")};
            var builder = new ProjectorBuilder<string, FakeMessageEnvelope>();
            builder.Register(projections).SetDependencyLifetimeScopeFactory(_factoryMock.Object);

            var projector = builder.Build<TestNextSequenceNumberRepository>();
            Assert.That(projector.Projections.Length, Is.EqualTo(2));
            Assert.That(projector.Projections[0], Is.EqualTo(projections[0]));
            Assert.That(projector.Projections[1], Is.EqualTo(projections[1]));
        }

        [Test]
        public void Build_WithCertainDependencyLifetimeScopeFactory_ShouldGetPassedToProjectorInstance()
        {
            var projection = new TestProjection("A");
            var builder = new ProjectorBuilder<string, FakeMessageEnvelope>();
            builder.Register(projection).SetDependencyLifetimeScopeFactory(_factoryMock.Object);

            var projector = builder.Build<TestNextSequenceNumberRepository>();
            Assert.That(projector.DependencyLifetimeScopeFactory, Is.EqualTo(_factoryMock.Object));
        }
    }
}
