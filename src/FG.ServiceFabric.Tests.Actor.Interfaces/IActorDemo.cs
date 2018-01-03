using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;

namespace FG.ServiceFabric.Tests.Actor.Interfaces
{
    public interface IActorDemo : IActor
    {
        Task<int> GetCountAsync();

        Task SetCountAsync(int count);
    }
}