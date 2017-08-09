using System;
using System.Collections.Generic;
using System.Fabric.Query;
using System.Linq;
using System.Reflection;
using System.Threading;
using FG.ServiceFabric.Testing.Mocks.Actors.Client;
using FG.ServiceFabric.Testing.Mocks.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Client;

namespace FG.ServiceFabric.Testing.Mocks.Services.Runtime
{
	internal class MockServiceInstance
	{
		public MockFabricRuntime FabricRuntime { get; private set; }

		public Uri ServiceUri { get; private set; }
		public Partition Partition { get; private set; }
		public Replica Replica { get; private set; }

		public IMockableServiceRegistration ServiceRegistration { get; private set; }
		public IMockableActorRegistration ActorRegistration { get; private set; }

		public object ServiceInstance { get; set; }

		internal virtual bool Equals(Uri serviceUri, Type serviceInterfaceType, ServicePartitionKey partitionKey)
		{
			if (ServiceRegistration == null) return false;

			var partitionId = ServiceRegistration.ServiceDefinition.GetPartion(partitionKey);

			return serviceUri.ToString().Equals(this.ServiceUri.ToString(), StringComparison.InvariantCultureIgnoreCase) &&
			       ServiceRegistration.InterfaceTypes.Any(i => i == serviceInterfaceType) &&
			       Partition.PartitionInformation.Id == partitionId;
		}

		internal virtual bool Equals(Type actorInterfaceType, ServicePartitionKey partitionKey)
		{
			return false;
		}


		protected virtual void Build()
		{
			if (ServiceInstance == null)
			{
				throw new NotSupportedException("Could not determine the type of service instance");
			}

			Run();
		}

		private void Run()
		{
			MethodInfo runAsyncMethod = null;
			Type serviceType = null;
			if (ServiceRegistration.IsStateful)
			{
				serviceType = typeof(Microsoft.ServiceFabric.Services.Runtime.StatefulServiceBase);
			}
			else
			{
				serviceType = typeof(Microsoft.ServiceFabric.Services.Runtime.StatelessService);
			}
			runAsyncMethod = serviceType.GetMethod("RunAsync", BindingFlags.Instance | BindingFlags.NonPublic);

			runAsyncMethod.Invoke(this.ServiceInstance, new object[] {FabricRuntime.CancellationToken});
		}

		public static IEnumerable<MockServiceInstance> Build(
			MockFabricRuntime fabricRuntime,
			IMockableActorRegistration actorRegistration
		)
		{
			var instances = new List<MockServiceInstance>();
			foreach (var replica in actorRegistration.ServiceRegistration.ServiceDefinition.Instances)
			{
				foreach (var partition in actorRegistration.ServiceRegistration.ServiceDefinition.Partitions)
				{
					var instance = new MockActorServiceInstance()
					{						
						ActorRegistration = actorRegistration,
						ServiceRegistration = actorRegistration.ServiceRegistration,
						FabricRuntime = fabricRuntime,
						Partition = partition,
						Replica = replica,
						ServiceUri = actorRegistration.ServiceRegistration.ServiceUri
					};
					instance.Build();
					instances.Add(instance);
				}
			}

			return instances;
		}

		public static IEnumerable<MockServiceInstance> Build(
			MockFabricRuntime fabricRuntime,
			IMockableServiceRegistration serviceRegistration
		)
		{
			var instances = new List<MockServiceInstance>();
			foreach (var replica in serviceRegistration.ServiceDefinition.Instances)
			{
				foreach (var partition in serviceRegistration.ServiceDefinition.Partitions)
				{
					MockServiceInstance instance = null;
					if (serviceRegistration.IsStateful)
					{
						instance = new MockStatefulServiceInstance()
						{
							ServiceRegistration = serviceRegistration,
							FabricRuntime = fabricRuntime,
							Partition = partition,
							Replica = replica,
							ServiceUri = serviceRegistration.ServiceUri
						};
					}
					else
					{
						instance = new MockStatelessServiceInstance()
						{
							ServiceRegistration = serviceRegistration,
							FabricRuntime = fabricRuntime,
							Partition = partition,
							Replica = replica,
							ServiceUri = serviceRegistration.ServiceUri
						};
					}
					
					instance.Build();
					instances.Add(instance);
				}
			}

			return instances;
		}
	}
}