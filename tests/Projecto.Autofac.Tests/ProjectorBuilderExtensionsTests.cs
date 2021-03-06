﻿using System;
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

            services.Register(ctx => new ProjectorBuilder<string, FakeMessageEnvelope>()
                .RegisterProjectionsFromAutofac(ctx)
                .UseAutofacDependencyLifetimeScopeFactory(ctx)
                .Build<FakeNextSequenceNumberRepository>())
                .AsSelf()
                .SingleInstance();

            var container = services.Build();

            var projector = container.Resolve<Projector<string, FakeMessageEnvelope, FakeNextSequenceNumberRepository>>();
            Assert.That(projector.Projections.Length, Is.EqualTo(2));
            Assert.That(projector.Projections.SingleOrDefault(x => x.GetType() == typeof(FakeProjectionA)), Is.Not.Null);
            Assert.That(projector.Projections.SingleOrDefault(x => x.GetType() == typeof(FakeProjectionB)), Is.Not.Null);
        }

        [Test]
        public void UseAutofac_CallingDependencyLifetimeScopeFactory_ShouldCreateAutofacDependencyLifetimeScope()
        {
            var services = new ContainerBuilder();

            services.RegisterType<FakeProjectionA>().AsImplementedInterfaces().SingleInstance();
            services.Register(ctx => new ProjectorBuilder<string, FakeMessageEnvelope>()
                .RegisterProjectionsFromAutofac(ctx)
                .UseAutofacDependencyLifetimeScopeFactory(ctx)
                .Build<FakeNextSequenceNumberRepository>())
                .AsSelf()
                .SingleInstance();

            var container = services.Build();

            var projector = container.Resolve<Projector<string, FakeMessageEnvelope, FakeNextSequenceNumberRepository>>();
            var scope = projector.DependencyLifetimeScopeFactory.BeginLifetimeScope();
            Assert.That(scope.GetType(), Is.EqualTo(typeof(AutofacDependencyLifetimeScope)));
        }

        [Test]
        public async Task UseAutofac_ProjectMessage_ShouldResolveConnection()
        {
            var sequence = 1;
            var messageEnvelope = new FakeMessageEnvelope(sequence, new FakeMessage());

            var projectionMock = new Mock<IProjection<string, FakeMessageEnvelope>>();
            projectionMock.SetupGet(x => x.Key).Returns("SomeKey");
            projectionMock.SetupGet(x => x.ConnectionType).Returns(typeof(FakeConnection));
            projectionMock
                .Setup(x => x.Handle(It.IsAny<Func<object>>(), messageEnvelope, It.IsAny<CancellationToken>()))
                .Callback<Func<object>, FakeMessageEnvelope, CancellationToken>(
                    (resolver, _, __) =>
                    {
                        var connection = resolver();
                        Assert.That(connection, Is.Not.Null);
                        Assert.That(connection.GetType(), Is.EqualTo(typeof(FakeConnection)));
                    })
                .Returns(() => Task.FromResult(true));

            var services = new ContainerBuilder();

            services.RegisterType<FakeConnection>().AsSelf().InstancePerLifetimeScope();
            services.RegisterType<FakeNextSequenceNumberRepository>().AsSelf().InstancePerLifetimeScope();
            services.RegisterInstance(projectionMock.Object).AsImplementedInterfaces().SingleInstance();
            services.Register(ctx => new ProjectorBuilder<string, FakeMessageEnvelope>()
                .RegisterProjectionsFromAutofac(ctx)
                .UseAutofacDependencyLifetimeScopeFactory(ctx)
                .Build<FakeNextSequenceNumberRepository>())
                .AsSelf()
                .SingleInstance();

            var container = services.Build();

            var projector = container.Resolve<Projector<string, FakeMessageEnvelope, FakeNextSequenceNumberRepository>>();
            // ReSharper disable once MethodSupportsCancellation
            await projector.Project(messageEnvelope);

            projectionMock.Verify(x => x.Handle(
                It.IsAny<Func<object>>(),
                It.IsAny<FakeMessageEnvelope>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void UseAutofacDependencyLifetimeScopeFactory_DisposeDependencyScope_ShouldNotDisposeParentAutofacLifetimeScope()
        {
            var services = new ContainerBuilder();

            services.RegisterType<FakeProjectionA>().AsImplementedInterfaces().SingleInstance();
            services.Register(ctx => new ProjectorBuilder<string, FakeMessageEnvelope>()
                .RegisterProjectionsFromAutofac(ctx)
                .UseAutofacDependencyLifetimeScopeFactory(ctx)
                .Build<FakeNextSequenceNumberRepository>())
                .AsSelf()
                .SingleInstance();

            var container = services.Build();

            var autofacLifetimeScopeDisposed = false;
            container.CurrentScopeEnding += (sender, args) => autofacLifetimeScopeDisposed = true;

            var projector = container.Resolve<Projector<string, FakeMessageEnvelope, FakeNextSequenceNumberRepository>>();
            projector.DependencyLifetimeScopeFactory.BeginLifetimeScope().Dispose();

            Assert.That(autofacLifetimeScopeDisposed, Is.False);
        }
    }
}
