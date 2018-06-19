namespace FG.ServiceFabric.Actors.Runtime
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IInnerActorState
    {
        Task<T> LoadStateAsync<T>(string stateName, CancellationToken cancellationToken = default(CancellationToken));

        Task<IEnumerable<string>> EnumerateStateNamesAsync(CancellationToken cancellationToken = default(CancellationToken));
    }
}