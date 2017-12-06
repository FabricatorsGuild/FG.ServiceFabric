using System;
using System.Collections.Generic;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using FG.ServiceFabric.Services.Runtime.StateSession;
using FG.ServiceFabric.Tests.StatefulServiceDemo;
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

		public StatefulServiceDemoBase(StatefulServiceContext context, IStateSessionManager stateSessionManager)
			: base(context)
		{
			_stateSessionManager = stateSessionManager;
		}

		public event EventHandler<RunAsyncLoopEventArgs> RunAsyncLoop;

		protected void OnRunAsyncLoop(int iteration)
		{
			RunAsyncLoop?.Invoke(this, new RunAsyncLoopEventArgs() {Context = this.Context, Iteration = iteration});
		}

		protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
		{
			return new ServiceReplicaListener[0];
		}
	}

	namespace With_simple_counter_state
	{
		public interface IStatefulServiceDemo : IService
		{
			Task RunWork();
		}

		public sealed class StatefulServiceDemo : StatefulServiceDemoBase, IStatefulServiceDemo
		{
			private readonly IStateSessionManager _stateSessionManager;

			public StatefulServiceDemo(StatefulServiceContext context, IStateSessionManager stateSessionManager)
				: base(context, stateSessionManager)
			{
				_stateSessionManager = stateSessionManager;
			}

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
			protected override async Task RunAsync(CancellationToken cancellationToken)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
			{
				OnRunAsyncLoop(0);
			}

			public async Task RunWork()
			{
				var cancellationToken = CancellationToken.None;
				await _stateSessionManager.OpenDictionary<long>("myDictionary", cancellationToken);

				cancellationToken.ThrowIfCancellationRequested();

				using (var session = _stateSessionManager.CreateSession())
				{
					var result = await session.TryGetValueAsync<long>("myDictionary", "Counter", cancellationToken);
					var value = result.HasValue ? result.Value : 0;
					await session.SetValueAsync("myDictionary", "Counter", value++, null, cancellationToken);
					await session.CommitAsync();
				}
			}
		}
	}

	namespace With_multiple_states
	{
		public interface IStatefulServiceDemo : IService
		{
			Task RunWork();
		}

		public sealed class StatefulServiceDemo : StatefulServiceDemoBase, IStatefulServiceDemo
		{
			private readonly IStateSessionManager _stateSessionManager;

			public StatefulServiceDemo(StatefulServiceContext context, IStateSessionManager stateSessionManager)
				: base(context, stateSessionManager)
			{
				_stateSessionManager = stateSessionManager;
			}

			protected override Task RunAsync(CancellationToken cancellationToken)
			{
				OnRunAsyncLoop(1);
				return Task.FromResult(true);
			}

			public async Task RunWork()
			{
				var cancellationToken = CancellationToken.None;
				var myDictionary = await _stateSessionManager.OpenDictionary<long>("myDictionary", cancellationToken);
				var dictionary2 = await _stateSessionManager.OpenDictionary<string>("myDictionary2", cancellationToken);

				using (var session = _stateSessionManager.CreateSession(myDictionary, dictionary2))
				{
					await dictionary2.SetValueAsync("theValue", "is hello", null, cancellationToken);
					await session.CommitAsync();
				}
				cancellationToken.ThrowIfCancellationRequested();

				using (var session = _stateSessionManager.CreateSession())
				{
					var result = await session.TryGetValueAsync<long>("myDictionary", "Counter", cancellationToken);
					var value = result.HasValue ? result.Value : 0;
					await session.SetValueAsync("myDictionary", "Counter", value++, null, cancellationToken);
					await session.CommitAsync();
				}


			}
		}
	}


	namespace With_polymorphic_array_state
	{
		public interface IStatefulServiceDemo : IService
		{
			Task RunWork();
		}

		public class ArrayState
		{
			public IInnerStateItem[] Items { get; set; }
		}

		public interface IInnerStateItem
		{
			string Name { get; set; }
		}

		public class InnerStateItemTypeA : IInnerStateItem
		{
			public string PropertyInA { get; set; }
			public string Name { get; set; }
		}

		public class InnerStateItemTypeB : IInnerStateItem
		{
			public string PropertyInB { get; set; }
			public string Name { get; set; }
		}

		public sealed class StatefulServiceDemo : StatefulServiceDemoBase, IStatefulServiceDemo
		{
			private readonly IStateSessionManager _stateSessionManager;

			public StatefulServiceDemo(StatefulServiceContext context, IStateSessionManager stateSessionManager)
				: base(context, stateSessionManager)
			{
				_stateSessionManager = stateSessionManager;
			}

			protected override Task RunAsync(CancellationToken cancellationToken)
			{				
				OnRunAsyncLoop(0);

				return Task.FromResult(true);
			}

			public async Task<ArrayState> GetStateAsync(CancellationToken cancellationToken)
			{
				await _stateSessionManager.OpenDictionary<ArrayState>("myDictionary2", cancellationToken);

				using (var session = _stateSessionManager.CreateSession())
				{
					var stateValue = await session.GetValueAsync<ArrayState>("myDictionary2", "theValue", cancellationToken);

					return stateValue;
				}
			}

			public async Task RunWork()
			{
				var cancellationToken = CancellationToken.None;

				await _stateSessionManager.OpenDictionary<ArrayState>("myDictionary2", cancellationToken);

				using (var session = _stateSessionManager.CreateSession())
				{
					var state = new ArrayState()
					{
						Items = new IInnerStateItem[]
						{
							new InnerStateItemTypeA() {Name = "I am a", PropertyInA = "Prop in A"},
							new InnerStateItemTypeB() {Name = "I am b", PropertyInB = "Prop in B"},
						}
					};


					await session.SetValueAsync("myDictionary2", "theValue", state, null, cancellationToken);
					await session.CommitAsync();
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

			Task<long> GetQueueLength();

			Task<long[]> EnumerateAll();
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

			public async Task Enqueue(int count)
			{
				var cancellationToken = CancellationToken.None;

				await _stateSessionManager.OpenQueue<long>("myQueue", cancellationToken);

				using (var session = _stateSessionManager.CreateSession(readOnly: false))
				{
					for (int i = 0; i < count; i++)
					{
						await session.EnqueueAsync("myQueue", _internalCounter++, null, cancellationToken);
					}
				}
			}

			public async Task<long[]> Dequeue(int count)
			{
				var cancellationToken = CancellationToken.None;

				await _stateSessionManager.OpenQueue<long>("myQueue", cancellationToken);

				var longs = new long[count];
				using (var session = _stateSessionManager.CreateSession(readOnly: false))
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

			public async Task<long> GetQueueLength()
			{
				var cancellationToken = CancellationToken.None;

				var myQueue = await _stateSessionManager.OpenQueue<long>("myQueue", cancellationToken);

				using (var session = _stateSessionManager.CreateSession(myQueue))
				{
					var value = await myQueue.GetCountAsync(cancellationToken);

					return value;
				}
			}

			public async Task<long[]> EnumerateAll()
			{
				var cancellationToken = CancellationToken.None;

				var results = new List<long>();
				var myQueue = await _stateSessionManager.OpenQueue<long>("myQueue", cancellationToken);

				using (var session = _stateSessionManager.CreateSession(myQueue))
				{
					var asyncEnumerable = await myQueue.CreateEnumerableAsync();
					var asyncEnumerator = asyncEnumerable.GetAsyncEnumerator();

					while (await asyncEnumerator.MoveNextAsync(cancellationToken))
					{
						results.Add(asyncEnumerator.Current);
					}

					return results.ToArray();
				}
			}

#pragma warning disable 1998
			protected override async Task RunAsync(CancellationToken cancellationToken)
#pragma warning restore 1998
			{
				OnRunAsyncLoop(0);
			}

			private T FastGetValue<T>(ConditionalValue<T> value)
			{
				return value.HasValue ? value.Value : default(T);
			}
		}
	}

	namespace With_simple_dictionary
	{
		public interface IStatefulServiceDemo : IService
		{
			Task Add(string key, string value);
			Task Remove(string key);
			Task<KeyValuePair<string, string>[]> EnumerateAll();
		}

		public sealed class StatefulServiceDemo : StatefulServiceDemoBase, IStatefulServiceDemo
		{
			private readonly IStateSessionManager _stateSessionManager;

			public StatefulServiceDemo(StatefulServiceContext context, IStateSessionManager stateSessionManager)
				: base(context, stateSessionManager)
			{
				_stateSessionManager = stateSessionManager;
			}

			public async Task Add(string key, string value)
			{
				var cancellationToken = CancellationToken.None;

				var myDict = await _stateSessionManager.OpenDictionary<string>("myDict", cancellationToken);

				using (var session = _stateSessionManager.CreateSession(myDict))
				{
					await myDict.SetValueAsync(key, value, cancellationToken);
					await session.CommitAsync();
				}
			}

			public async Task Remove(string key)
			{
				var cancellationToken = CancellationToken.None;

				var myDict = await _stateSessionManager.OpenDictionary<string>("myDict", cancellationToken);

				using (var session = _stateSessionManager.CreateSession(myDict))
				{
					await myDict.RemoveAsync(key, cancellationToken);
					await session.CommitAsync();
				}
			}

			public async Task<KeyValuePair<string, string>[]> EnumerateAll()
			{
				var cancellationToken = CancellationToken.None;

				var results = new List<KeyValuePair<string, string>>();
				var myDict = await _stateSessionManager.OpenDictionary<string>("myDict", cancellationToken);

				using (var session = _stateSessionManager.CreateSession(myDict))
				{
					var asyncEnumerable = await myDict.CreateEnumerableAsync();
					var asyncEnumerator = asyncEnumerable.GetAsyncEnumerator();

					while (await asyncEnumerator.MoveNextAsync(cancellationToken))
					{
						results.Add(asyncEnumerator.Current);
					}

					return results.ToArray();
				}
			}

#pragma warning disable 1998
			protected override async Task RunAsync(CancellationToken cancellationToken)
#pragma warning restore 1998
			{
				OnRunAsyncLoop(0);
			}
		}
	}
}