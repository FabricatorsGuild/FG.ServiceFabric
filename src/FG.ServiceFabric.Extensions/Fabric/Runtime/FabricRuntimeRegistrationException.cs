using System;
using System.Runtime.Serialization;

namespace FG.ServiceFabric.Fabric.Runtime
{
	[Serializable]
	public class FabricRuntimeRegistrationException : Exception
	{
		public FabricRuntimeRegistrationException()
		{
		}

		public FabricRuntimeRegistrationException(string message) : base(message)
		{
		}

		public FabricRuntimeRegistrationException(string message, Exception inner) : base(message, inner)
		{
		}

		protected FabricRuntimeRegistrationException
		(
			SerializationInfo info,
			StreamingContext context) : base(info, context)
		{
		}

		public static FabricRuntimeRegistrationException CouldNotCreateServiceRuntimeException(string name, Exception ex)
		{
			return new FabricRuntimeRegistrationException($"Could not create the service runtime {name}", ex);
		}

		public static FabricRuntimeRegistrationException CouldNotFindeServiceRuntimeTypeException(string typeName)
		{
			return new FabricRuntimeRegistrationException($"Could not find a type with the name {typeName}");
		}
	}
}