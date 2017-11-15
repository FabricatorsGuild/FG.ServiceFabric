using System;

namespace FG.ServiceFabric.Actors.Remoting.FabricTransport
{
	[AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
	sealed class FabricTransportRemotingExceptionHandlerAttribute : Attribute
	{
		public FabricTransportRemotingExceptionHandlerAttribute(Type exceptionHandlerType)
		{
			ExceptionHandlerType = exceptionHandlerType;
		}

		public Type ExceptionHandlerType { get; }
	}
}