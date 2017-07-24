using System;
using System.Linq;
using System.Threading.Tasks;

namespace FG.ServiceFabric.DocumentDb
{
    public interface IDocumentDbStateReader : IDisposable
    {
        Task<IQueryable<T>> QueryAsync<T>() where T : IPersistedIdentity;
    }

    public interface IDocumentDbStateWriter : IDisposable
    {
        Task UpsertAsync<T>(T state, string stateName) where T : IPersistedIdentity;
    }
}