using System;
using Moq;
using NUnit.Framework;
using Projecto.Infrastructure;
using Projecto.Tests.TestClasses;

namespace Projecto.Tests
{
    [TestFixture]
    public class ProjectorBuilderTests
    {
        [Test]
        public void Register_PassingNullAsSingleProjection_ShouldThrowException()
        {
            var builder = new ProjectorBuilder<FakeProjectContext>();
            var ex = Assert.Throws<ArgumentNullException>(() => builder.Register((Projection<FakeConnection, FakeProjectContext>)null));
            Assert.That(ex.ParamName, Is.EqualTo("projection"));
        }

        [Test]
        public void Register_PassingNullAsMultipleProjections_ShouldThrowException()
        {
            var builder = new ProjectorBuilder<FakeProjectContext>();
            var ex = Assert.Throws<ArgumentNullException>(() => builder.Register((Projection<FakeConnection, FakeProjectContext>[])null));
            Assert.That(ex.ParamName, Is.EqualTo("projections"));
        }

        [Test]
        public void SetConnectionResolver_PassingNullAsConnectionResolver_ShouldThrowException()
        {
            var builder = new ProjectorBuilder<FakeProjectContext>();
            var ex = Assert.Throws<ArgumentNullException>(() => builder.SetConnectionResolver(null));
            Assert.That(ex.ParamName, Is.EqualTo("connectionResolver"));
        }

        [Test]
        public void Build_WithSingleProjectionRegistered_ShouldGetPassedToProjectorInstance()
        {
            var connectionResolver = new Mock<IConnectionResolver>().Object;
            var projection = new TestProjection();
            var builder = new ProjectorBuilder<FakeProjectContext>();
            builder.Register(projection).SetConnectionResolver(connectionResolver);

            var projector = builder.Build();
            Assert.That(projector.Projections.Length, Is.EqualTo(1));
            Assert.That(projector.Projections[0], Is.EqualTo(projection));
        }

        [Test]
        public void Build_WithMultipleProjectionsRegistered_ShouldGetPassedToProjectorInstance()
        {
            var connectionResolver = new Mock<IConnectionResolver>().Object;
            var projections = new[] {new TestProjection(), new TestProjection()};
            var builder = new ProjectorBuilder<FakeProjectContext>();
            builder.Register(projections).SetConnectionResolver(connectionResolver);

            var projector = builder.Build();
            Assert.That(projector.Projections.Length, Is.EqualTo(2));
            Assert.That(projector.Projections[0], Is.EqualTo(projections[0]));
            Assert.That(projector.Projections[1], Is.EqualTo(projections[1]));
        }

        [Test]
        public void Build_WithCertainConnectionResolver_ShouldGetPassedToProjectorInstance()
        {
            var connectionResolver = new Mock<IConnectionResolver>().Object;
            var projection = new TestProjection();
            var builder = new ProjectorBuilder<FakeProjectContext>();
            builder.Register(projection).SetConnectionResolver(connectionResolver);

            var projector = builder.Build();
            Assert.That(projector.ConnectionResolver, Is.EqualTo(connectionResolver));
        }
    }
}
