using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;

namespace FG.ServiceFabric.Tests.Actor.Interfaces
{
	public interface IActorWithReminderDemo : IActor
	{
		Task<int> GetCountAsync();
	}
}