using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Moq;
using NUnit.Framework;
using Projecto.Autofac.Tests.TestClasses;
using Projecto.Infrastructure;

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
                .RegisterFromAutofac(ctx)
                .UseAutofacProjectScopeFactory(ctx)
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
        public void UseAutofac_CallingProjectScopeFactory_ShouldCreateAutofacProjectScope()
        {
            var services = new ContainerBuilder();

            services.RegisterType<FakeProjectionA>().AsImplementedInterfaces().SingleInstance();
            services.Register(ctx => new ProjectorBuilder<FakeMessageEnvelope>()
                .RegisterFromAutofac(ctx)
                .UseAutofacProjectScopeFactory(ctx)
                .Build())
                .AsSelf()
                .SingleInstance();

            var container = services.Build();

            var projector = container.Resolve<Projector<FakeMessageEnvelope>>();
            var scope = projector.ProjectScopeFactory(new[] { new FakeMessageEnvelope(1, new FakeMessage()) });
            Assert.That(scope.GetType(), Is.EqualTo(typeof(AutofacProjectScope)));
        }

        [Test]
        public async Task UseAutofac_ProjectMessage_ShouldResolveConnection()
        {
            var sequence = 1;
            var messageEnvelope = new FakeMessageEnvelope(sequence, new FakeMessage());

            var projectionMock = new Mock<IProjection<FakeMessageEnvelope>>();
            projectionMock.Setup(x => x.NextSequenceNumber).Returns(() => sequence);
            projectionMock
                .Setup(x => x.Handle(It.IsAny<Func<Type, object>>(), messageEnvelope, It.IsAny<CancellationToken>()))
                .Callback<Func<Type, object>, FakeMessageEnvelope, CancellationToken>(
                    (resolver, _, __) =>
                    {
                        var connection = resolver(typeof(FakeConnection));
                        Assert.That(connection, Is.Not.Null);
                        Assert.That(connection.GetType(), Is.EqualTo(typeof(FakeConnection)));
                        sequence++;
                    })
                .Returns(() => Task.FromResult(0));

            var services = new ContainerBuilder();

            services.RegisterType<FakeConnection>().AsSelf().InstancePerLifetimeScope();
            services.RegisterInstance(projectionMock.Object).AsImplementedInterfaces().SingleInstance();
            services.Register(ctx => new ProjectorBuilder<FakeMessageEnvelope>()
                .RegisterFromAutofac(ctx)
                .UseAutofacProjectScopeFactory(ctx)
                .Build())
                .AsSelf()
                .SingleInstance();

            var container = services.Build();

            var projector = container.Resolve<Projector<FakeMessageEnvelope>>();
            await projector.Project(messageEnvelope);

            projectionMock.Verify(x => x.Handle(
                It.IsAny<Func<Type, object>>(),
                It.IsAny<FakeMessageEnvelope>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
