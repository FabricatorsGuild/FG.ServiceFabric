using System.Collections.Generic;
using System.Threading.Tasks;

namespace FG.ServiceFabric.Services.Runtime.StateSession.CosmosDb
{
	public interface IStateQuerySession
	{
		Task<IEnumerable<string>> GetServices();
		Task<IEnumerable<string>> GetPartitions(string service);
		Task<IEnumerable<string>> GetStates(string service, string partition);
		Task<IEnumerable<string>> GetActors(string service, string partition);
		Task<IEnumerable<string>> GetActorReminders(string service, string partition, string actor);
	}
}