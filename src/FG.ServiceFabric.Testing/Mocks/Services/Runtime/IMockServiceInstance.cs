﻿using System;
using System.Fabric.Query;

namespace FG.ServiceFabric.Testing.Mocks.Services.Runtime
{
	public interface IMockServiceInstance
	{
		MockActorServiceInstanceStatus Status { get; }
		Uri ServiceUri { get;  }
		Partition Partition { get; }
		Replica Replica { get; }

		DateTime? RunAsyncStarted { get; }
		DateTime? RunAsyncEnded { get; }
	}
}