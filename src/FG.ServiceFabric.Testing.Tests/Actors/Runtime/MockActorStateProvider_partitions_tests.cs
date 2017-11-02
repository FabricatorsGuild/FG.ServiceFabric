using System;
using System.Collections.Generic;
using System.Fabric;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using FG.Common.Async;
using FG.ServiceFabric.Testing.Mocks;
using FG.ServiceFabric.Testing.Mocks.Services.Runtime;
using FG.ServiceFabric.Tests.Actor;
using FG.ServiceFabric.Tests.Actor.Interfaces;
using FG.ServiceFabric.Tests.Actor.WithInteralError;
using FluentAssertions;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Services.Client;
using NUnit.Framework;

namespace FG.ServiceFabric.Testing.Tests.Actors.Runtime
{
	// ReSharper disable InconsistentNaming
	public class MockActorStateProvider_partitions_tests
	{
		private string ApplicationName => @"Overlord";
		private MockFabricRuntime _fabricRuntime;
		private MockFabricApplication _fabricApplication;
		private MockServiceDefinition _actorDemoServiceDefinition;

		[SetUp]
#pragma warning disable 1998
		public async Task CreateActorsWithActorService()
#pragma warning restore 1998
		{
			_fabricRuntime = new MockFabricRuntime();
			_fabricApplication = _fabricRuntime.RegisterApplication(ApplicationName);

			 _actorDemoServiceDefinition = MockServiceDefinition.CreateUniformInt64Partitions(10, long.MinValue, long.MaxValue);
			_fabricApplication.SetupActor(
				(service, actorId) => new ActorDemo(service, actorId),
				(context, actorTypeInformation, stateProvider, stateManagerFactory) =>
					new ActorDemoActorService(context, actorTypeInformation, stateProvider: stateProvider),
				serviceDefinition: _actorDemoServiceDefinition);
		}

		[Test]
#pragma warning disable 1998
		public async Task MockServiceDefinition_should_create_in64_ranges()
#pragma warning restore 1998
		{
			var instanceCount = 10;
			var lowKey = new BigInteger(long.MinValue);
			var highKey = new BigInteger(long.MaxValue);
			var uniformInt64Partitions = MockServiceDefinition.CreateUniformInt64Partitions(instanceCount, (long)lowKey, (long)highKey);

			var low = lowKey;
			var partitionRange = (highKey- lowKey) / instanceCount;
			foreach (var partition in uniformInt64Partitions.Partitions)
			{
				var int64Range = (partition.PartitionInformation as Int64RangePartitionInformation);

				//int64Range.LowKey.Should().Be((long)low);
				//int64Range.HighKey.Should().Be(lowKey + partitionRange);
				low = int64Range.HighKey;

				Console.WriteLine($"{int64Range.LowKey} - {int64Range.HighKey} = {int64Range.Id}");
			}
		}

		[Test]
#pragma warning disable 1998
		public async Task MockServiceDefinition_should_split_actors_into_separate_partitions()
#pragma warning restore 1998
		{
			var uniformInt64Partitions = MockServiceDefinition.CreateUniformInt64Partitions(10, long.MinValue, long.MaxValue);
		
			var partitionsUsed = new List<Guid>();
			foreach (var actorName in new string[] { "first", "second", "third" })
			{
				var actorId = new ActorId(actorName);
				var partitionKey = actorId.GetPartitionKey();
				var partitionId = uniformInt64Partitions.GetPartion(new ServicePartitionKey(partitionKey));

				partitionsUsed.Should().NotContain(partitionId);

				partitionsUsed.Add(partitionId);

				Console.WriteLine($"{actorId}\t{partitionKey}\t{partitionId}");
			}
		}


		[Test]
		public async Task Should_be_able_to_remove_actor_from_ActorService()
		{
			var serviceUri = _fabricApplication.ApplicationUriBuilder.Build("ActorDemoActorService").ToUri();

			var nameCount = 100;
			foreach (var name in new []{"first", "second", "third"})
			{
				await ExecutionHelper.ExecuteWithRetriesAsync((ct) =>
				{
					var actor = _fabricRuntime.ActorProxyFactory.CreateActorProxy<IActorDemo>(new ActorId(name));
					return actor.SetCountAsync(nameCount);
				}, 3, TimeSpan.FromMilliseconds(3), CancellationToken.None);
				nameCount += 100;
			}
			
			await ExecutionHelper.ExecuteWithRetriesAsync((ct) =>
			{
				var proxy = _fabricRuntime.ActorProxyFactory.CreateActorServiceProxy<IActorDemoActorService>(
					serviceUri,
					new ActorId("first"));
				return proxy.RemoveAsync(new ActorId("first"), CancellationToken.None);
			}, 3, TimeSpan.FromMilliseconds(3), CancellationToken.None);

			var counts = new List<int>();

			IList<Int64RangePartitionInformation> partitionKeys = new List<Int64RangePartitionInformation>();
			var partitionListAsync = await _fabricRuntime.PartitionEnumerationManager.GetPartitionListAsync(serviceUri);
			foreach (var partition in partitionListAsync)
			{
				var partitionInfo = partition.PartitionInformation as Int64RangePartitionInformation;
				if (partitionInfo == null)
				{
					throw new InvalidOperationException($"The service {serviceUri} should have a uniform Int64 partition. Instead: {partition.PartitionInformation.Kind}");
				}
				partitionKeys.Add(partitionInfo);
			}

			foreach (var partitionKey in partitionKeys)
			{
				await ExecutionHelper.ExecuteWithRetriesAsync(async (ct) =>
				{
					var proxy = _fabricRuntime.ActorProxyFactory.CreateActorServiceProxy<IActorDemoActorService>(
						serviceUri, partitionKey.LowKey);
					var partitionCounts = await proxy.GetCountsAsync(ct);
					foreach (var count in partitionCounts)
					{
						counts.Add(count);
					}
					return Task.FromResult(true);
				}, 3, TimeSpan.FromMilliseconds(3), CancellationToken.None);
			}

			counts.Should().BeEquivalentTo(new[] {200, 300});
		}
	}
	// ReSharper restore InconsistentNaming
}