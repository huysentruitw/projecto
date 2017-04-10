using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Moq;
using NUnit.Framework;
using Projecto.Autofac.Tests.TestClasses;

namespace Projecto.Autofac.Tests
{
    [TestFixture]
    public class ProjectorBuilderExtensionsTests
    {
        [Test]
        public void UseAutofac_PassingContainerContainingProjections_ShouldRegisterProjections()
        {
            var services = new ContainerBuilder();

            services.RegisterType<FakeProjectionA>().AsImplementedInterfaces().SingleInstance();
            services.RegisterType<FakeProjectionB>().AsImplementedInterfaces().SingleInstance();

            services.Register(ctx => new ProjectorBuilder<FakeMessageEnvelope>()
                .RegisterProjectionsFromAutofac(ctx)
                .UseAutofacConnectionLifetimeScopeFactory(ctx)
                .Build())
                .AsSelf()
                .SingleInstance();

            var container = services.Build();

            var projector = container.Resolve<Projector<FakeMessageEnvelope>>();
            Assert.That(projector.Projections.Length, Is.EqualTo(2));
            Assert.That(projector.Projections.SingleOrDefault(x => x.GetType() == typeof(FakeProjectionA)), Is.Not.Null);
            Assert.That(projector.Projections.SingleOrDefault(x => x.GetType() == typeof(FakeProjectionB)), Is.Not.Null);
        }

        [Test]
        public void UseAutofac_CallingConnectionLifetimeScopeFactory_ShouldCreateAutofacConnectionLifetimeScope()
        {
            var services = new ContainerBuilder();

            services.RegisterType<FakeProjectionA>().AsImplementedInterfaces().SingleInstance();
            services.Register(ctx => new ProjectorBuilder<FakeMessageEnvelope>()
                .RegisterProjectionsFromAutofac(ctx)
                .UseAutofacConnectionLifetimeScopeFactory(ctx)
                .Build())
                .AsSelf()
                .SingleInstance();

            var container = services.Build();

            var projector = container.Resolve<Projector<FakeMessageEnvelope>>();
            var scope = projector.ConnectionLifetimeScopeFactory.BeginLifetimeScope();
            Assert.That(scope.GetType(), Is.EqualTo(typeof(AutofacConnectionLifetimeScope)));
        }

        [Test]
        public async Task UseAutofac_ProjectMessage_ShouldResolveConnection()
        {
            var sequence = 1;
            var messageEnvelope = new FakeMessageEnvelope(sequence, new FakeMessage());

            var projectionMock = new Mock<IProjection<FakeMessageEnvelope>>();
            projectionMock.Setup(x => x.GetNextSequenceNumber()).Returns(() => sequence);
            projectionMock.SetupGet(x => x.ConnectionType).Returns(typeof(FakeConnection));
            projectionMock
                .Setup(x => x.Handle(It.IsAny<Func<object>>(), messageEnvelope, It.IsAny<CancellationToken>()))
                .Callback<Func<object>, FakeMessageEnvelope, CancellationToken>(
                    (resolver, _, __) =>
                    {
                        var connection = resolver();
                        Assert.That(connection, Is.Not.Null);
                        Assert.That(connection.GetType(), Is.EqualTo(typeof(FakeConnection)));
                        sequence++;
                    })
                .Returns(() => Task.FromResult(0));

            var services = new ContainerBuilder();

            services.RegisterType<FakeConnection>().AsSelf().InstancePerLifetimeScope();
            services.RegisterInstance(projectionMock.Object).AsImplementedInterfaces().SingleInstance();
            services.Register(ctx => new ProjectorBuilder<FakeMessageEnvelope>()
                .RegisterProjectionsFromAutofac(ctx)
                .UseAutofacConnectionLifetimeScopeFactory(ctx)
                .Build())
                .AsSelf()
                .SingleInstance();

            var container = services.Build();

            var projector = container.Resolve<Projector<FakeMessageEnvelope>>();
            await projector.Project(messageEnvelope);

            projectionMock.Verify(x => x.Handle(
                It.IsAny<Func<object>>(),
                It.IsAny<FakeMessageEnvelope>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void UseAutofacConnectionLifetimeScopeFactory_DisposeConnectionScope_ShouldNotDisposeParentAutofacLifetimeScope()
        {
            var services = new ContainerBuilder();

            services.RegisterType<FakeProjectionA>().AsImplementedInterfaces().SingleInstance();
            services.Register(ctx => new ProjectorBuilder<FakeMessageEnvelope>()
                .RegisterProjectionsFromAutofac(ctx)
                .UseAutofacConnectionLifetimeScopeFactory(ctx)
                .Build())
                .AsSelf()
                .SingleInstance();

            var container = services.Build();

            var autofacLifetimeScopeDisposed = false;
            container.CurrentScopeEnding += (sender, args) => autofacLifetimeScopeDisposed = true;

            var projector = container.Resolve<Projector<FakeMessageEnvelope>>();
            projector.ConnectionLifetimeScopeFactory.BeginLifetimeScope().Dispose();

            Assert.That(autofacLifetimeScopeDisposed, Is.False);
        }
    }
}
