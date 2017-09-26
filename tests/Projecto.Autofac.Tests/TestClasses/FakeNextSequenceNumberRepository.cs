using System.Collections.Generic;
using System.Threading.Tasks;

namespace Projecto.Autofac.Tests.TestClasses
{
    public class FakeNextSequenceNumberRepository : INextSequenceNumberRepository<string>
    {
        public Task<IReadOnlyDictionary<string, int>> Fetch(ISet<string> projectionKeys) => Task.FromResult<IReadOnlyDictionary<string, int>>(new Dictionary<string, int>());

        public Task Store(IReadOnlyDictionary<string, int> nextSequenceNumbers) => Task.FromResult(false);
    }
}
