using System;
using System.Collections.Generic;

namespace FG.ServiceFabric.Fabric.Runtime
{
	public class FabricRuntimeContextWrapper : IDisposable
	{
		private readonly bool _shouldDispose = false;

		public FabricRuntimeContextWrapper()
		{
			if (FabricRuntimeContext.Current == null)
			{
				_shouldDispose = true;
				FabricRuntimeContext.Current = new FabricRuntimeContext();
			}
		}

		public static FabricRuntimeContextWrapper Current
		{
			get
			{
				if (FabricRuntimeContext.Current == null) return null;

				return new FabricRuntimeContextWrapper();
			}
		}

		public IServiceRuntimeRegistration ServiceRuntimeRegistration
		{
			get => (IServiceRuntimeRegistration) FabricRuntimeContext.Current?[
				FabricRuntimeContextKeys.ServiceRuntimeRegistration];
			set
			{
				if (FabricRuntimeContext.Current != null)
				{
					FabricRuntimeContext.Current[FabricRuntimeContextKeys.ServiceRuntimeRegistration] = value;
				}
			}
		}

		public object this[string index]
		{
			get => FabricRuntimeContext.Current?[index];
			set => FabricRuntimeContext.Current[index] = value;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (_shouldDispose)
				{
					FabricRuntimeContext.Current = null;
				}
			}
		}

		public IEnumerable<string> GetAllKeys()
		{
			return FabricRuntimeContext.Current?.Keys ?? new string[0];
		}

		private static class FabricRuntimeContextKeys
		{
			public const string ServiceRuntimeRegistration = "ServiceRuntimeRegistration";
		}
	}
}