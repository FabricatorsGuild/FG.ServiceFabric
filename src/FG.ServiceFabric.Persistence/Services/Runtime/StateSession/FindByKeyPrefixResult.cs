using System.Collections.Generic;
using Microsoft.ServiceFabric.Actors.Query;

namespace FG.ServiceFabric.Services.Runtime.StateSession
{
    public class FindByKeyPrefixResult
    {
        public IEnumerable<string> Items { get; set; }
        public ContinuationToken ContinuationToken { get; set; }
    }

    public class FindByKeyPrefixResult<T>
    {
        public IEnumerable<KeyValuePair<string, T>> Items { get; set; }
        public ContinuationToken ContinuationToken { get; set; }
    }
}