namespace FG.ServiceFabric.Services.Runtime.StateSession.CosmosDb
{
	public interface IStateQuerySessionManager
	{
		IStateQuerySession CreateSession();
	}
}