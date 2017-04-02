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
        private readonly ProjectScopeFactory<FakeMessageEnvelope> _projectScopeFactory = _ => new FakeProjectScope();

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
        public void SetProjectScopeFactory_PassingNullAsProjectScopeFactory_ShouldThrowException()
        {
            var builder = new ProjectorBuilder<FakeMessageEnvelope>();
            var ex = Assert.Throws<ArgumentNullException>(() => builder.SetProjectScopeFactory(null));
            Assert.That(ex.ParamName, Is.EqualTo("projectScopeFactory"));
        }

        [Test]
        public void Build_WithSingleProjectionRegistered_ShouldGetPassedToProjectorInstance()
        {
            var projection = new TestProjection();
            var builder = new ProjectorBuilder<FakeMessageEnvelope>();
            builder.Register(projection).SetProjectScopeFactory(_projectScopeFactory);

            var projector = builder.Build();
            Assert.That(projector.Projections.Length, Is.EqualTo(1));
            Assert.That(projector.Projections[0], Is.EqualTo(projection));
        }

        [Test]
        public void Build_WithMultipleProjectionsRegistered_ShouldGetPassedToProjectorInstance()
        {
            var projectScopeFactory = new Mock<ProjectScopeFactory<FakeMessageEnvelope>>().Object;
            var projections = new[] {new TestProjection(), new TestProjection()};
            var builder = new ProjectorBuilder<FakeMessageEnvelope>();
            builder.Register(projections).SetProjectScopeFactory(projectScopeFactory);

            var projector = builder.Build();
            Assert.That(projector.Projections.Length, Is.EqualTo(2));
            Assert.That(projector.Projections[0], Is.EqualTo(projections[0]));
            Assert.That(projector.Projections[1], Is.EqualTo(projections[1]));
        }

        [Test]
        public void Build_WithCertainProjectScopeFactory_ShouldGetPassedToProjectorInstance()
        {
            var projection = new TestProjection();
            var builder = new ProjectorBuilder<FakeMessageEnvelope>();
            builder.Register(projection).SetProjectScopeFactory(_projectScopeFactory);

            var projector = builder.Build();
            Assert.That(projector.ProjectScopeFactory, Is.EqualTo(_projectScopeFactory));
        }
    }
}
