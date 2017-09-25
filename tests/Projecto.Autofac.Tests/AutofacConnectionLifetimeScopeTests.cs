using System;
using System.Diagnostics.CodeAnalysis;
using Autofac;
using Autofac.Core;
using Moq;
using NUnit.Framework;
using Projecto.Autofac.Tests.TestClasses;
using Projecto.DependencyInjection;

namespace Projecto.Autofac.Tests
{
    [TestFixture]
    [SuppressMessage("ReSharper", "ObjectCreationAsStatement")]
    public class AutofacConnectionLifetimeScopeTests
    {
        [Test]
        public void Constructor_PassNullAsILifetimeScope_ShouldThrowException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => new AutofacConnectionLifetimeScope(null));
            Assert.That(ex.ParamName, Is.EqualTo("lifetimeScope"));
        }

        [Test]
        public void Dispose_WithUsingStatement_ShouldFireScopeEndingEvent()
        {
            var lifetimeScopeMock = new Mock<ILifetimeScope>();
            var scopeEndingCalled = 0;

            using (var scope = new AutofacConnectionLifetimeScope(lifetimeScopeMock.Object))
                scope.ScopeEnding += e => scopeEndingCalled++;

            Assert.That(scopeEndingCalled, Is.EqualTo(1));
        }

        [Test]
        public void Dispose_CalledMultipleTimes_ShouldFireScopeEndingEventOnlyOnce()
        {
            var lifetimeScopeMock = new Mock<ILifetimeScope>();
            var scopeEndingCalled = 0;

            var scope = new AutofacConnectionLifetimeScope(lifetimeScopeMock.Object);
            scope.ScopeEnding += e => scopeEndingCalled++;
            scope.Dispose();
            scope.Dispose();

            Assert.That(scopeEndingCalled, Is.EqualTo(1));
        }

        [Test]
        public void Dispose_WithUsingStatement_ShouldAlsoDisposeAutofacLifetimeScope()
        {
            var lifetimeScopeMock = new Mock<ILifetimeScope>();

            using (new AutofacConnectionLifetimeScope(lifetimeScopeMock.Object)) { }

            lifetimeScopeMock.Verify(x => x.Dispose(), Times.Once);
        }

        [Test]
        public void Construction_NewScope_ShouldHaveEmptyPropertiesDictionary()
        {
            var lifetimeScopeMock = new Mock<ILifetimeScope>();
            var scope = new AutofacConnectionLifetimeScope(lifetimeScopeMock.Object);
            Assert.That(scope.Properties, Is.Not.Null);
            Assert.That(scope.Properties.Count, Is.EqualTo(0));
        }

        [Test]
        public void ResolveConnection_PassingSomeConnectionType_ShouldResolveFromAutofacLifetimeScope()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<FakeConnection>();
            var container = builder.Build();

            var scope = new AutofacConnectionLifetimeScope(container);
            var connection = scope.ResolveConnection<FakeConnection>();

            Assert.That(connection, Is.Not.Null);
            Assert.That(connection.GetType(), Is.EqualTo(typeof(FakeConnection)));
        }

        [Test]
        public void ResolveConnection_PassingKnownConnectionType_ShouldFireConnectionResolvedEvent()
        {
            ConnectionResolvedEventArgs eventArgs = null;

            var builder = new ContainerBuilder();
            builder.RegisterType<FakeConnection>();
            var container = builder.Build();

            var scope = new AutofacConnectionLifetimeScope(container);
            scope.ConnectionResolved += e =>
            {
                Assert.That(eventArgs, Is.Null);
                eventArgs = e;
            };

            Assert.That(scope.ResolveConnection<FakeConnection>(), Is.Not.Null);

            Assert.That(eventArgs, Is.Not.Null);
            Assert.That(eventArgs.LifetimeScope, Is.EqualTo(scope));
            Assert.That(eventArgs.ConnectionType, Is.EqualTo(typeof(FakeConnection)));
            Assert.That(eventArgs.Connection.GetType(), Is.EqualTo(typeof(FakeConnection)));
        }

        [Test]
        public void ResolveConnection_PassingConnectionTypeThatResolvesAsNull_ShouldNotFireConnectionResolvedEvent()
        {
            var eventFired = false;

            var builder = new ContainerBuilder();
            builder.Register<FakeConnection>(ctx => null);
            var container = builder.Build();

            var scope = new AutofacConnectionLifetimeScope(container);
            scope.ConnectionResolved += e => eventFired = true;

            Assert.Throws<DependencyResolutionException>(() => scope.ResolveConnection<FakeConnection>());

            Assert.That(eventFired, Is.False);
        }
    }
}
