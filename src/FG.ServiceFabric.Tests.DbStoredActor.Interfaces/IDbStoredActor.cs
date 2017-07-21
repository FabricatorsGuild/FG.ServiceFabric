using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;

namespace FG.ServiceFabric.Tests.DbStoredActor.Interfaces
{
    public interface IDbStoredActor : IActor
    {
        Task<CountState> GetCountAsync(CancellationToken cancellationToken);
        Task SetCountAsync(CountState count, CancellationToken cancellationToken);
    }

    [DataContract]
    public class CountState
    {
        [DataMember]
        public int Count { get; set; }
    }
}
