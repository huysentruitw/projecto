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

            services.Register(ctx => new ProjectorBuilder<FakeProjectContext>()
                .RegisterFromAutofac(ctx)
                .UseAutofacProjectScopeFactory(ctx)
                .Build())
                .AsSelf()
                .SingleInstance();

            var container = services.Build();

            var projector = container.Resolve<Projector<FakeProjectContext>>();
            Assert.That(projector.Projections.Length, Is.EqualTo(2));
            Assert.That(projector.Projections.SingleOrDefault(x => x.GetType() == typeof(FakeProjectionA)), Is.Not.Null);
            Assert.That(projector.Projections.SingleOrDefault(x => x.GetType() == typeof(FakeProjectionB)), Is.Not.Null);
        }

        [Test]
        public void UseAutofac_CallingProjectScopeFactory_ShouldCreateAutofacProjectScope()
        {
            var services = new ContainerBuilder();

            services.RegisterType<FakeProjectionA>().AsImplementedInterfaces().SingleInstance();
            services.Register(ctx => new ProjectorBuilder<FakeProjectContext>()
                .RegisterFromAutofac(ctx)
                .UseAutofacProjectScopeFactory(ctx)
                .Build())
                .AsSelf()
                .SingleInstance();

            var container = services.Build();

            var projector = container.Resolve<Projector<FakeProjectContext>>();
            var scope = projector.ProjectScopeFactory(new FakeProjectContext(), new FakeMessage());
            Assert.That(scope.GetType(), Is.EqualTo(typeof(AutofacProjectScope)));
        }

        [Test]
        public async Task UseAutofac_ProjectMessage_ShouldResolveConnection()
        {
            var message = new FakeMessage();
            var projectContext = new FakeProjectContext();

            var sequence = 1;
            var projectionMock = new Mock<IProjection<FakeProjectContext>>();
            projectionMock.Setup(x => x.NextSequenceNumber).Returns(() => sequence);
            projectionMock
                .Setup(x => x.Handle(It.IsAny<Func<Type, object>>(), projectContext, message, It.IsAny<CancellationToken>()))
                .Callback<Func<Type, object>, FakeProjectContext, object, CancellationToken>(
                    (resolver, _, __, ___) =>
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
            services.Register(ctx => new ProjectorBuilder<FakeProjectContext>()
                .RegisterFromAutofac(ctx)
                .UseAutofacProjectScopeFactory(ctx)
                .Build())
                .AsSelf()
                .SingleInstance();

            var container = services.Build();

            var projector = container.Resolve<Projector<FakeProjectContext>>();
            await projector.Project(1, projectContext, message);

            projectionMock.Verify(x => x.Handle(
                It.IsAny<Func<Type, object>>(),
                It.IsAny<FakeProjectContext>(),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
