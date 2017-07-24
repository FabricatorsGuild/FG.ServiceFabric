using System.Linq;
using System.Threading.Tasks;

namespace FG.ServiceFabric.DocumentDb
{
    public interface IDocumentDbReader
    {
        Task<IQueryable<T>> QueryAsync<T>() where T : IPersistedIdentity;
    }
}