using System;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;

namespace FG.ServiceFabric.Tests.Actor.Interfaces
{
    public interface IEventStoredActor : IActor
    {
        Task CreateAsync(Guid aggregateRootId, string value);
        Task UpdateAsync(Guid aggregateRootId, string value);
    }
}