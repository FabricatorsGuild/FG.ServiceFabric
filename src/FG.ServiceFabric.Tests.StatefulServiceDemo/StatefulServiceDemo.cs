﻿using System;
using System.Collections.Generic;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using FG.ServiceFabric.Services.Runtime.StateSession;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Runtime;

namespace FG.ServiceFabric.Tests.StatefulServiceDemo
{

	public class RunAsyncLoopEventArgs
	{
		public StatefulServiceContext Context { get; set; }
		public int Iteration { get; set; }
	}

	public abstract class StatefulServiceDemoBase : StatefulService
	{
		private readonly IStateSessionManager _stateSessionManager;

		public event EventHandler<RunAsyncLoopEventArgs> RunAsyncLoop;

		protected void OnRunAsyncLoop(int iteration)
		{
			RunAsyncLoop?.Invoke(this, new RunAsyncLoopEventArgs() { Context = this.Context, Iteration = iteration });
		}

		public StatefulServiceDemoBase(StatefulServiceContext context, IStateSessionManager stateSessionManager)
			: base(context)
		{
			_stateSessionManager = stateSessionManager;
		}

		protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
		{
			return new ServiceReplicaListener[0];
		}
	}

	namespace With_simple_counter_state
	{
		public sealed class StatefulServiceDemo : StatefulServiceDemoBase
		{
			private readonly IStateSessionManager _stateSessionManager;

			public StatefulServiceDemo(StatefulServiceContext context, IStateSessionManager stateSessionManager)
				: base(context, stateSessionManager)
			{
				_stateSessionManager = stateSessionManager;
			}

			protected override async Task RunAsync(CancellationToken cancellationToken)
			{
				await _stateSessionManager.OpenDictionary<long>("myDictionary", cancellationToken);

				var i = 0;
				while (true)
				{
					cancellationToken.ThrowIfCancellationRequested();

					using (var session = _stateSessionManager.CreateSession())
					{
						var result = await session.TryGetValueAsync<long>("myDictionary", "Counter", cancellationToken);
						var value = result.HasValue ? result.Value : 0;
						await session.SetValueAsync("myDictionary", "Counter", value++, null, cancellationToken);
					}

					OnRunAsyncLoop(i);
					i++;
				}
			}
		}
	}

	namespace With_multiple_states
	{
		public sealed class StatefulServiceDemo : StatefulServiceDemoBase
		{
			private readonly IStateSessionManager _stateSessionManager;

			public StatefulServiceDemo(StatefulServiceContext context, IStateSessionManager stateSessionManager)
				: base(context, stateSessionManager)
			{
				_stateSessionManager = stateSessionManager;
			}

			protected override async Task RunAsync(CancellationToken cancellationToken)
			{
				await _stateSessionManager.OpenDictionary<long>("myDictionary", cancellationToken);
				await _stateSessionManager.OpenDictionary<string>("myDictionary2", cancellationToken);

				using (var session = _stateSessionManager.CreateSession())
				{
					await session.SetValueAsync("myDictionary2", "theValue", "is hello", null, cancellationToken);
				}

				var i = 0;
				while (true)
				{
					cancellationToken.ThrowIfCancellationRequested();

					using (var session = _stateSessionManager.CreateSession())
					{
						var result = await session.TryGetValueAsync<long>("myDictionary", "Counter", cancellationToken);
						var value = result.HasValue ? result.Value : 0;
						await session.SetValueAsync("myDictionary", "Counter", value++, null, cancellationToken);
					}

					OnRunAsyncLoop(i);
					i++;
				}
			}
		}
	}

	namespace With_simple_queue_enqueued
	{
		public interface IStatefulServiceDemo : IService
		{
			Task Enqueue(int count);
			Task<long[]> Dequeue(int count);
			Task<bool> Peek();
		}

		public sealed class StatefulServiceDemo : StatefulServiceDemoBase, IStatefulServiceDemo
		{
			private readonly IStateSessionManager _stateSessionManager;

			private long _internalCounter = 1L;

			public StatefulServiceDemo(StatefulServiceContext context, IStateSessionManager stateSessionManager)
				: base(context, stateSessionManager)
			{
				_stateSessionManager = stateSessionManager;
			}

			protected override async Task RunAsync(CancellationToken cancellationToken)
			{
				OnRunAsyncLoop(0);
			}

			public async Task Enqueue(int count)
			{
				var cancellationToken = CancellationToken.None;

				await _stateSessionManager.OpenQueue<long>("myQueue", cancellationToken);

				using (var session = _stateSessionManager.CreateSession())
				{
					for (int i = 0; i < count; i++)
					{
						await session.EnqueueAsync("myQueue",_internalCounter++, null, cancellationToken);
					}
				}
			}

			private T FastGetValue<T>(ConditionalValue<T> value)
			{
				return value.HasValue ? value.Value : default(T);
			}

			public async Task<long[]> Dequeue(int count)
			{
				var cancellationToken = CancellationToken.None;

				await _stateSessionManager.OpenQueue<long>("myQueue", cancellationToken);

				var longs = new long[count];
				using (var session = _stateSessionManager.CreateSession())
				{
					for (int i = 0; i < count; i++)
					{
						longs[i] = FastGetValue(await session.DequeueAsync<long>("myQueue", cancellationToken));
					}
				}

				return longs;
			}

			public async Task<bool> Peek()
			{
				var cancellationToken = CancellationToken.None;

				await _stateSessionManager.OpenQueue<long>("myQueue", cancellationToken);

				using (var session = _stateSessionManager.CreateSession())
				{
					var value = await session.PeekAsync<long>("myQueue", cancellationToken);

					return value.HasValue;
				}
			}			
		}
	}
}