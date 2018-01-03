using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using FG.ServiceFabric.DocumentDb;
using Microsoft.ServiceFabric.Actors;

namespace FG.ServiceFabric.Tests.DbStoredActor.Interfaces
{
    public interface IDbStoredActor : IActor
    {
        Task<CountReadModel> GetCountAsync(CancellationToken cancellationToken);
        Task SetCountAsync(int count, CancellationToken cancellationToken);
    }

    [DataContract]
    public class CountReadModel : IPersistedIdentity
    {
        [DataMember]
        public int Count { get; set; }

        [DataMember]
        public string Id { get; set; }
    }

    [DataContract]
    public class CountState : IPersistedIdentity
    {
        private CountState()
        {
        }

        public CountState(string id)
        {
            Id = id;
        }

        [DataMember]
        public int Count { get; private set; }

        [DataMember]
        public string Id { get; private set; }

        public CountState UpdateCount(int count)
        {
            return new CountState(Id) {Count = count};
        }
    }
}