using System.Threading;
using System.Threading.Tasks;

namespace FG.ServiceFabric.Services.Runtime.StateSession
{
    public interface IStateSessionManager
    {
        Task<IStateSessionReadOnlyDictionary<T>> OpenDictionary<T>(string schema,
            CancellationToken cancellationToken = default(CancellationToken));

        Task<IStateSessionReadOnlyQueue<T>> OpenQueue<T>(string schema,
            CancellationToken cancellationToken = default(CancellationToken));
		
        IStateSessionReader CreateSession(params IStateSessionReadOnlyObject[] stateSessionObjects);

        IStateSessionWritableManager Writable { get; }
    }
}