using System.Collections.Generic;
using System.Threading.Tasks;

namespace FG.ServiceFabric.Services.Runtime.StateSession.CosmosDb
{
	public interface IDocumentDbDataManager
	{
		string GetCollectionName();

		Task<IDictionary<string, string>> GetCollectionDataAsync(string collectionName);

		Task CreateCollection(string collectionName);

		Task DestroyCollecton(string collectionName);
	}
}