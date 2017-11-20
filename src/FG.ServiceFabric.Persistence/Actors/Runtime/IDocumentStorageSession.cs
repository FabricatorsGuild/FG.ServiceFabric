using System.Collections.Generic;
using System.Threading.Tasks;

namespace FG.ServiceFabric.Actors.Runtime
{
	public interface IDocumentStorageSession
	{
		Task<bool> ContainsAsync(string key);

		Task UpsertAsync(string key, ExternalState value);

		Task<ExternalState> ReadAsync(string key);

		Task DeleteAsync(string key);

		Task<IEnumerable<string>> FindByKeyAsync(string keyPrefix, int numOfItemsToReturn = -1, object marker = null);
	}
}