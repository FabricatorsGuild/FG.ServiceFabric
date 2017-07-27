using System;
using System.Linq;
using System.Threading.Tasks;

namespace FG.ServiceFabric.DocumentDb
{
    // TODO: Support queries.
    public interface IDocumentDbStateReader : IDisposable
    {
        Task<T> ReadAsync<T>(string id, Guid partitionKey) where T : IPersistedIdentity;
    }
}