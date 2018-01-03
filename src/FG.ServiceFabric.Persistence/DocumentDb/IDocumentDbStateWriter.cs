using System;
using System.Threading.Tasks;

namespace FG.ServiceFabric.DocumentDb
{
    public interface IDocumentDbStateWriter : IDisposable
    {
        Task UpsertAsync<T>(T state, IStateMetadata metadata) where T : IPersistedIdentity;
        Task DelecteAsync(string id, Guid partitionKey);
    }
}