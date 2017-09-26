using System;
using System.Diagnostics.CodeAnalysis;
using Autofac;
using NUnit.Framework;
using Projecto.Autofac.Tests.TestClasses;
using Projecto.DependencyInjection;

namespace Projecto.Autofac.Tests
{
    [TestFixture]
    [SuppressMessage("ReSharper", "ObjectCreationAsStatement")]
    public class AutofacDependencyLifetimeScopeFactoryTests
    {
        private Func<ILifetimeScope> _autofacLifetimeScopeFactory;

        [SetUp]
        public void SetUp()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<FakeConnection>();
            var container = builder.Build();
            _autofacLifetimeScopeFactory = () => container.BeginLifetimeScope();
        }

        [Test]
        public void Constructor_PassingNullAsAutofacLifetimeScopeFactory_ShouldThrowException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => new AutofacDependencyLifetimeScopeFactory(null));
            Assert.That(ex.ParamName, Is.EqualTo("autofacLifetimeScopeFactory"));
        }

        [Test]
        public void BeginLifetimeScope_CallTheMethodTwice_ShouldReturnANewScopeEachTime()
        {
            var factory = new AutofacDependencyLifetimeScopeFactory(_autofacLifetimeScopeFactory);
            var scope1 = factory.BeginLifetimeScope();
            var scope2 = factory.BeginLifetimeScope();
            Assert.That(scope1, Is.Not.Null);
            Assert.That(scope2, Is.Not.Null);
            Assert.That(scope1, Is.Not.EqualTo(scope2));
        }

        [Test]
        public void BeginLifetimeScope_CallOnce_ShouldForwardDependencyResolvedEvent()
        {
            DependencyResolvedEventArgs eventArgs = null;
            var factory = new AutofacDependencyLifetimeScopeFactory(_autofacLifetimeScopeFactory);
            factory.ChildScopeDependencyResolved += e => { Assert.That(eventArgs, Is.Null); eventArgs = e; };
            var scope = factory.BeginLifetimeScope();

            Assert.That(eventArgs, Is.Null);
            scope.Resolve(typeof(FakeConnection));

            Assert.That(eventArgs, Is.Not.Null);
            Assert.That(eventArgs.LifetimeScope, Is.EqualTo(scope));
            Assert.That(eventArgs.DependencyType, Is.EqualTo(typeof(FakeConnection)));
            Assert.That(eventArgs.Dependency, Is.Not.Null);
            Assert.That(eventArgs.Dependency.GetType(), Is.EqualTo(typeof(FakeConnection)));
        }

        [Test]
        public void BeginLifetimeScope_CallOnce_ShouldForwardScopeEndingEvent()
        {
            DependencyLifetimeScopeEndingEventArgs eventArgs = null;
            var factory = new AutofacDependencyLifetimeScopeFactory(_autofacLifetimeScopeFactory);
            factory.ChildScopeEnding += e => { Assert.That(eventArgs, Is.Null); eventArgs = e; };
            var scope = factory.BeginLifetimeScope();

            Assert.That(eventArgs, Is.Null);
            scope.Dispose();

            Assert.That(eventArgs, Is.Not.Null);
            Assert.That(eventArgs.LifetimeScope, Is.EqualTo(scope));
        }
    }
}
