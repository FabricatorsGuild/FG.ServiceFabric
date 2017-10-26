using System;
using System.Collections.Generic;
using System.Fabric;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using FG.Common.Utils;
using FG.ServiceFabric.Fabric.Runtime;
using FG.ServiceFabric.Testing.Mocks.Services.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;

namespace FG.ServiceFabric.Testing.Mocks {
	public class MockServiceRuntimeRegistration : IServiceRuntimeRegistration
	{
		private readonly string _applicationName;
		private readonly Assembly _serviceAssembly;
		private readonly IDictionary<string, MockServiceDefinition> _serviceDefinitions;

		public MockServiceRuntimeRegistration(string applicationName, Assembly serviceAssembly, IDictionary<string, MockServiceDefinition> serviceDefinitions)
		{
			_applicationName = applicationName;
			_serviceAssembly = serviceAssembly;
			_serviceDefinitions = serviceDefinitions;
		}

		public IDictionary<string, MockServiceDefinition> ServiceDefinitions => _serviceDefinitions;

		public Task RegisterServiceAsync
		(
			string serviceTypeName,
			Func<StatelessServiceContext, StatelessService> serviceFactory,
			TimeSpan timeout = default(TimeSpan),
			CancellationToken cancellationToken = default(CancellationToken))
		{
			var serviceName = serviceTypeName.RemoveFromEnd("Type");

			var typeName = $"{_serviceAssembly.FullName}.{serviceName}";
			var type = _serviceAssembly.GetType(typeName);
			if(type == null)
			{
				throw MockServiceRuntimeException.CouldNotFindServiceTypeInServiceAssembly(typeName, _serviceAssembly);
			}

			var serviceType = serviceFactory.Method.ReturnType;
			System.Diagnostics.Debug.Assert(serviceType == type);

			var serviceDefinitions = _serviceDefinitions[serviceTypeName];
			MockFabricRuntime.Current.SetupService(_applicationName, serviceFactory, serviceDefinitions);

			return Task.FromResult(true);
		}

		public Task RegisterServiceAsync
		(
			string serviceTypeName,
			Func<StatefulServiceContext, StatefulServiceBase> serviceFactory,
			TimeSpan timeout = default(TimeSpan),
			CancellationToken cancellationToken = default(CancellationToken))
		{
			var serviceName = serviceTypeName.RemoveFromEnd("Type");

			var typeName = $"{_serviceAssembly.FullName}.{serviceName}";
			var type = _serviceAssembly.GetType(typeName);
			if (type == null)
			{
				throw MockServiceRuntimeException.CouldNotFindServiceTypeInServiceAssembly(typeName, _serviceAssembly);
			}

			var serviceType = serviceFactory.Method.ReturnType;	
			System.Diagnostics.Debug.Assert(serviceType == type);

			var serviceDefinitions = _serviceDefinitions[serviceTypeName];
			MockFabricRuntime.Current.SetupService(
				applicationName: _applicationName,
				createService: (context, replica2) => serviceFactory(context), 
				createStateManager: null,
				serviceDefinition: serviceDefinitions);

			return Task.FromResult(true);
		}
	}

	[Serializable]
	public class MockServiceRuntimeException : Exception
	{
		public static MockServiceRuntimeException CouldNotFindServiceTypeInServiceAssembly(string serviceTypeName, Assembly assembly)
		{
			return new MockServiceRuntimeException($"Could not find service CLR type {serviceTypeName} in assembly {assembly.FullName}");
		}

		public MockServiceRuntimeException() { }
		public MockServiceRuntimeException(string message) : base(message) { }
		public MockServiceRuntimeException(string message, Exception inner) : base(message, inner) { }
		protected MockServiceRuntimeException
		(
			SerializationInfo info,
			StreamingContext context) : base(info, context) { }
	}
}