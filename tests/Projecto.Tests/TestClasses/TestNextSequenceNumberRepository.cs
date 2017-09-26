using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Projecto.Tests.TestClasses
{
    public class TestNextSequenceNumberRepository : Dictionary<string, int>, INextSequenceNumberRepository<string>
    {
        public int NumberOfFetchCalls { get; private set; } = 0;

        public int NumberOfStoreCalls { get; private set; } = 0;

        public Task<IReadOnlyDictionary<string, int>> Fetch(ISet<string> projectionKeys)
        {
            NumberOfFetchCalls = NumberOfFetchCalls + 1;
            var result = projectionKeys.Where(x => ContainsKey(x)).ToDictionary(x => x, x => this[x]);
            return Task.FromResult<IReadOnlyDictionary<string, int>>(result);
        }

        public Task Store(IReadOnlyDictionary<string, int> nextSequenceNumbers)
        {
            NumberOfStoreCalls = NumberOfStoreCalls + 1;
            foreach (var kvp in nextSequenceNumbers)
            {
                if (ContainsKey(kvp.Key)) this[kvp.Key] = kvp.Value;
                else Add(kvp.Key, kvp.Value);
            }
            return Task.FromResult(false);
        }
    }
}
