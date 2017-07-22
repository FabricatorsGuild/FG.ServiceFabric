using System.Reflection;
using Microsoft.ServiceFabric.Actors;

namespace FG.ServiceFabric.Testing.Mocks.Actors.Client
{
	public interface IMockActorProxyManager
	{
		void BeforeMethod(IActor actor, MethodInfo method);
		void AfterMethod(IActor actor, MethodInfo method);
	}
}