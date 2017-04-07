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
        private readonly Mock<IConnectionLifetimeScopeFactory> _factoryMock;

        public ProjectorBuilderTests()
        {
            _factoryMock = new Mock<IConnectionLifetimeScopeFactory>();
            _factoryMock.Setup(x => x.BeginLifetimeScope()).Returns(() => new Mock<IConnectionLifetimeScope>().Object);
        }

        [Test]
        public void Register_PassingNullAsProjection_ShouldThrowException()
        {
            var builder = new ProjectorBuilder<FakeMessageEnvelope>();
            var ex = Assert.Throws<ArgumentNullException>(() => builder.Register((IProjection<FakeMessageEnvelope>)null));
            Assert.That(ex.ParamName, Is.EqualTo("projection"));
        }

        [Test]
        public void Register_PassingNullAsMultipleProjections_ShouldThrowException()
        {
            var builder = new ProjectorBuilder<FakeMessageEnvelope>();
            var ex = Assert.Throws<ArgumentNullException>(() => builder.Register((Projection<FakeConnection, FakeMessageEnvelope>[])null));
            Assert.That(ex.ParamName, Is.EqualTo("projections"));
        }

        [Test]
        public void SetConnectionLifetimeScopeFactory_PassingNullAsConnectionLifetimeScopeFactory_ShouldThrowException()
        {
            var builder = new ProjectorBuilder<FakeMessageEnvelope>();
            var ex = Assert.Throws<ArgumentNullException>(() => builder.SetConnectionLifetimeScopeFactory(null));
            Assert.That(ex.ParamName, Is.EqualTo("factory"));
        }

        [Test]
        public void Build_WithSingleProjectionRegistered_ShouldGetPassedToProjectorInstance()
        {
            var projection = new TestProjection();
            var builder = new ProjectorBuilder<FakeMessageEnvelope>();
            builder.Register(projection).SetConnectionLifetimeScopeFactory(_factoryMock.Object);

            var projector = builder.Build();
            Assert.That(projector.Projections.Length, Is.EqualTo(1));
            Assert.That(projector.Projections[0], Is.EqualTo(projection));
        }

        [Test]
        public void Build_WithMultipleProjectionsRegistered_ShouldGetPassedToProjectorInstance()
        {
            var projections = new[] {new TestProjection(), new TestProjection()};
            var builder = new ProjectorBuilder<FakeMessageEnvelope>();
            builder.Register(projections).SetConnectionLifetimeScopeFactory(_factoryMock.Object);

            var projector = builder.Build();
            Assert.That(projector.Projections.Length, Is.EqualTo(2));
            Assert.That(projector.Projections[0], Is.EqualTo(projections[0]));
            Assert.That(projector.Projections[1], Is.EqualTo(projections[1]));
        }

        [Test]
        public void Build_WithCertainConnectionLifetimeScopeFactory_ShouldGetPassedToProjectorInstance()
        {
            var projection = new TestProjection();
            var builder = new ProjectorBuilder<FakeMessageEnvelope>();
            builder.Register(projection).SetConnectionLifetimeScopeFactory(_factoryMock.Object);

            var projector = builder.Build();
            Assert.That(projector.ConnectionLifetimeScopeFactory, Is.EqualTo(_factoryMock.Object));
        }
    }
}
