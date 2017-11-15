using System;
using System.Reflection;
using System.Runtime.Serialization;

namespace FG.ServiceFabric.Testing.Mocks
{
	[Serializable]
	public class MockServiceRuntimeException : Exception
	{
		public MockServiceRuntimeException()
		{
		}

		public MockServiceRuntimeException(string message) : base(message)
		{
		}

		public MockServiceRuntimeException(string message, Exception inner) : base(message, inner)
		{
		}

		protected MockServiceRuntimeException
		(
			SerializationInfo info,
			StreamingContext context) : base(info, context)
		{
		}

		public static MockServiceRuntimeException CouldNotFindServiceTypeInServiceAssembly(string serviceTypeName,
			Assembly assembly)
		{
			return new MockServiceRuntimeException(
				$"Could not find service CLR type {serviceTypeName} in assembly {assembly.FullName}");
		}
	}
}