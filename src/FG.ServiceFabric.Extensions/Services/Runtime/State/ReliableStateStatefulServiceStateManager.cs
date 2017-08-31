using Microsoft.ServiceFabric.Data;

namespace FG.ServiceFabric.Services.Runtime.State
{
	public class ReliableStateStatefulServiceStateManager : IStatefulServiceStateManager
	{
		private readonly IReliableStateManager _innerStateManagerReplica;
		public ReliableStateStatefulServiceStateManager(IReliableStateManager innerStateManagerReplica)
		{
			_innerStateManagerReplica = innerStateManagerReplica;
		}

		public virtual IStatefulServiceStateManagerSession CreateSession()
		{
			return new ReliableStateStatefulServiceStateManagerSession(_innerStateManagerReplica);
		}
	}
}