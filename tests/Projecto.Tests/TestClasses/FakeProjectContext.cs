using System;

namespace Projecto.Tests.TestClasses
{
    public class FakeProjectContext
    {
        public string OriginatingCommandId { get; } = Guid.NewGuid().ToString("N");

        public DateTime DateCreated { get; } = DateTime.UtcNow;
    }
}
