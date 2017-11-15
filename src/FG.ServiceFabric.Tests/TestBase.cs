using FG.ServiceFabric.Services.Runtime;
using FG.ServiceFabric.Testing.Mocks;
using FG.ServiceFabric.Tests.CQRS;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using NUnit.Framework;

namespace FG.ServiceFabric.Tests
{
	public abstract class TestBase
	{
		protected MockFabricApplication _fabricApplication;
		protected MockFabricRuntime FabricRuntime;
		private string ApplicationName => @"Overlord";

		protected IServiceProxyFactory ServiceProxyFactory => FabricRuntime.ServiceProxyFactory;
		protected IActorProxyFactory ActorProxyFactory => FabricRuntime.ActorProxyFactory;

		[SetUp]
		public void Setup()
		{
			FabricRuntime = new MockFabricRuntime();
			_fabricApplication = FabricRuntime.RegisterApplication(ApplicationName);
			// ReSharper disable once VirtualMemberCallInConstructor
			SetupRuntime();
		}

		protected virtual void SetupRuntime()
		{
		}
	}
}