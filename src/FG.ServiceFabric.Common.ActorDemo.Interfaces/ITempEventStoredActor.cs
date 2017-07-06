using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;

namespace FG.ServiceFabric.Tests.Actor.Interfaces
{
    public interface ITempEventStoredActor : IActor
    {
        Task GiveBirthAsync(RegisterCommand command);
        Task MarryAsync(MarryCommand command);
    }

    public interface ITempEventStoredActorService : FG.ServiceFabric.Actors.Runtime.IEventStoredActorService
    {
    }
}