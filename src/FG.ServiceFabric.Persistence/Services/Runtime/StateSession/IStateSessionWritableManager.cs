using System.Threading;
using System.Threading.Tasks;

namespace FG.ServiceFabric.Services.Runtime.StateSession
{
    public interface IStateSessionWritableManager
    {
        Task<IStateSessionDictionary<T>> OpenDictionary<T>(string schema,
            CancellationToken cancellationToken = default(CancellationToken));

        Task<IStateSessionQueue<T>> OpenQueue<T>(string schema,
            CancellationToken cancellationToken = default(CancellationToken));

        IStateSession CreateSession(params IStateSessionObject[] stateSessionObjects);
    }
}