namespace FG.ServiceFabric.Services.Runtime.StateSession.CosmosDb
{
	public interface ILoggerFactory
	{
		IDocumentDbStateSessionManagerLogger CreateLogger(IStateSessionManager stateSessionManager);
		IDocumentDbStateSessionLogger CreateLogger(IStateSessionManager stateSessionManager, IStateSession stateSession);
	}
}