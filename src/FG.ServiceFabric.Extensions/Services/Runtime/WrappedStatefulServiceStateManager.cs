using FG.ServiceFabric.Services.Runtime.State;

namespace FG.ServiceFabric.Services.Runtime
{
	public class WrappedStatefulServiceStateManager : IStatefulServiceStateManager
	{
		private readonly IStatefulServiceStateManager _innerServiceStateManager;

		public WrappedStatefulServiceStateManager(IStatefulServiceStateManager innerServiceStateManager)
		{
			_innerServiceStateManager = innerServiceStateManager;
		}

		public virtual IStatefulServiceStateManagerSession CreateSession()
		{
			var innerSession = _innerServiceStateManager.CreateSession();
			return innerSession;
		}		
	}
}