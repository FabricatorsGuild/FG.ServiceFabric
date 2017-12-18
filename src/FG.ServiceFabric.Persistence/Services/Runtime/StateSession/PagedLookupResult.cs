using System.Collections.Generic;
using Microsoft.ServiceFabric.Actors.Query;

namespace FG.ServiceFabric.Services.Runtime.StateSession
{
    public sealed class PagedLookupResult<TKey, T>
    {
        public IEnumerable<KeyValuePair<TKey, T>> Items { get; set; }
        public ContinuationToken ContinuationToken { get; set; }
    }
}