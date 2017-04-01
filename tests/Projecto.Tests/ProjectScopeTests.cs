using System;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Projecto.Tests.TestClasses;

namespace Projecto.Tests
{
    [TestFixture]
    public class ProjectScopeTests
    {
        [Test]
        public async Task BeforeDispose_ResolvedOneConnection_ShouldOnlyCallDisposalCallbackForResolvedConnection()
        {
            var callbacksMock = new Mock<TestConnectionDisposalCallbacks>();
            var scope = new TestProjectScope(callbacksMock.Object);

            var connectionType = typeof(ConnectionB);
            var connection = scope.InternalResolveConnection(connectionType);
            Assert.That(connection.GetType(), Is.EqualTo(connectionType));

            await scope.BeforeDispose();

            callbacksMock.Verify(x => x.ResolvedA(), Times.Never);
            callbacksMock.Verify(x => x.ResolvedB(), Times.Once);
        }

        [Test]
        public async Task BeforeDispose_ResolvedSameConnectionTwice_ShouldOnlyCallDisposalCallbackForResolvedConnectionOnce()
        {
            var callbacksMock = new Mock<TestConnectionDisposalCallbacks>();
            var scope = new TestProjectScope(callbacksMock.Object);

            var connectionType = typeof(ConnectionB);
            var connection = scope.InternalResolveConnection(connectionType);
            Assert.That(connection.GetType(), Is.EqualTo(connectionType));

            connection = scope.InternalResolveConnection(connectionType);
            Assert.That(connection.GetType(), Is.EqualTo(connectionType));

            await scope.BeforeDispose();

            callbacksMock.Verify(x => x.ResolvedA(), Times.Never);
            callbacksMock.Verify(x => x.ResolvedB(), Times.Once);
        }

        [Test]
        public async Task BeforeDispose_ResolvedTwoDifferentConnections_ShouldCallDisposalCallbackForBothResolvedConnections()
        {
            var callbacksMock = new Mock<TestConnectionDisposalCallbacks>();
            var scope = new TestProjectScope(callbacksMock.Object);

            var connection = scope.InternalResolveConnection(typeof(ConnectionA));
            Assert.That(connection.GetType(), Is.EqualTo(typeof(ConnectionA)));

            connection = scope.InternalResolveConnection(typeof(ConnectionB));
            Assert.That(connection.GetType(), Is.EqualTo(typeof(ConnectionB)));

            await scope.BeforeDispose();

            callbacksMock.Verify(x => x.ResolvedA(), Times.Once);
            callbacksMock.Verify(x => x.ResolvedB(), Times.Once);
        }

        public class TestProjectScope : ProjectScope
        {
            private readonly ConnectionA _connectionA = new ConnectionA();
            private readonly ConnectionB _connectionB = new ConnectionB();

            public TestProjectScope(ConnectionDisposalCallbacks connectionDisposalCallbacks)
                : base(connectionDisposalCallbacks)
            { }

            public override void Dispose() { }

            public override object ResolveConnection(Type connectionType)
            {
                if (connectionType == typeof(ConnectionA)) return _connectionA;
                if (connectionType == typeof(ConnectionB)) return _connectionB;
                throw new Exception($"Connection with type {connectionType} unknown");
            }
        }

        public abstract class TestConnectionDisposalCallbacks : ConnectionDisposalCallbacks
        {
            public TestConnectionDisposalCallbacks()
            {
                BeforeDisposalOf<ConnectionA>(connection => Task.Run(() => ResolvedA()));
                BeforeDisposalOf<ConnectionB>(connection => Task.Run(() => ResolvedB()));
            }

            public abstract void ResolvedA();

            public abstract void ResolvedB();
        }
    }
}
