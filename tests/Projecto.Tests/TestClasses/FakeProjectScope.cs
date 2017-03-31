using System;

namespace Projecto.Tests.TestClasses
{
    public class FakeProjectScope : ProjectScope
    {
        public override void Dispose() { }

        public override object ResolveConnection(Type connectionType) => new FakeConnection();
    }
}
