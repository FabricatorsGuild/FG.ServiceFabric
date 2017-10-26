using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;

namespace FG.ServiceFabric.Fabric.Runtime
{

	// ReSharper disable once ClassNeverInstantiated.Global - Created in using classes
	public sealed class FabricRuntimeContext
	{
		private static readonly string ContextKey = Guid.NewGuid().ToString();

		public FabricRuntimeContext()
		{
			_values = new ConcurrentDictionary<string, object>();
		}

		private readonly IDictionary<string, object> _values;

		public object this[string index]
		{
			get => _values.ContainsKey(index) ? _values[index] : null;
			set => _values[index] = value;
		}

		public IEnumerable<string> Keys => _values.Keys;

		public static FabricRuntimeContext Current
		{
			get => (FabricRuntimeContext)CallContext.LogicalGetData(ContextKey);
			internal set
			{
				if (value == null)
				{
					CallContext.FreeNamedDataSlot(ContextKey);
				}
				else
				{
					CallContext.LogicalSetData(ContextKey, value);
				}
			}
		}
	}
}