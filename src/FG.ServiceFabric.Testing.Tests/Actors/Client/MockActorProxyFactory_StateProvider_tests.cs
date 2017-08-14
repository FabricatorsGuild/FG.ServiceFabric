using System;
using System.Threading;
using System.Threading.Tasks;
using FG.Common.Async;
using FG.ServiceFabric.Tests.Actor;
using FG.ServiceFabric.Testing.Mocks;
using FG.ServiceFabric.Testing.Mocks.Services.Runtime;
using FG.ServiceFabric.Tests.Actor.Interfaces;
using FluentAssertions;
using Microsoft.ServiceFabric.Actors;
using NUnit.Framework;
// ReSharper disable InconsistentNaming

namespace FG.ServiceFabric.Testing.Tests.Actors.Client
{
	public class MockActorStateProvider_tests
	{
		private IActorDemoActorService _proxy;
		private MockFabricRuntime _fabricRuntime;

		[SetUp]
		public async Task CreateActorsWithActorService()
		{
			_fabricRuntime = new MockFabricRuntime("Overlord");

			_fabricRuntime.SetupActor<ActorDemo, ActorDemoActorService>(
				(service, actorId) => new ActorDemo(service, actorId),
				(context, actorTypeInformation, stateProvider, stateManagerFactory) =>
					new ActorDemoActorService(context, actorTypeInformation, stateProvider: stateProvider),
				serviceDefinition: MockServiceDefinition.CreateUniformInt64Partitions(1, long.MinValue, long.MaxValue));

			await ExecutionHelper.ExecuteWithRetriesAsync((ct) =>
			{
				var actor = _fabricRuntime.ActorProxyFactory.CreateActorProxy<IActorDemo>(new ActorId("first"));
				return actor.SetCountAsync(1);
			}, 3, TimeSpan.FromMilliseconds(3), CancellationToken.None);

			await ExecutionHelper.ExecuteWithRetriesAsync((ct) =>
			{
				var actor = _fabricRuntime.ActorProxyFactory.CreateActorProxy<IActorDemo>(new ActorId("second"));
				return actor.SetCountAsync(2);
			}, 3, TimeSpan.FromMilliseconds(3), CancellationToken.None);

			await ExecutionHelper.ExecuteWithRetriesAsync((ct) =>
			{
				var actor = _fabricRuntime.ActorProxyFactory.CreateActorProxy<IActorDemo>(new ActorId("third"));
				return actor.SetCountAsync(3);
			}, 3, TimeSpan.FromMilliseconds(3), CancellationToken.None);

			_proxy = _fabricRuntime.ActorProxyFactory.CreateActorServiceProxy<IActorDemoActorService>(
				_fabricRuntime.ApplicationUriBuilder.Build("ActorDemoActorService").ToUri(), new ActorId("testivus"));
		}


		[Test]
		public async Task Should_be_able_to_get_actor_state_from_Actor()
		{
			var result = await ExecutionHelper.ExecuteWithRetriesAsync((ct) =>
			{
				var actor = _fabricRuntime.ActorProxyFactory.CreateActorProxy<IActorDemo>(new ActorId("first"));
				return actor.GetCountAsync();
			}, 3, TimeSpan.FromMilliseconds(3), CancellationToken.None);

			result.Should().Be(1);
		}

		[Test]
		public async Task Should_be_able_to_get_actor_state_from_ActorService()
		{
			var result = await ExecutionHelper.ExecuteWithRetriesAsync((ct) =>
			{
				return _proxy.GetCountAsync(new ActorId("second"), CancellationToken.None);

			}, 3, TimeSpan.FromMilliseconds(3), CancellationToken.None);

			result.Should().Be(2);
		}

		[Test]
		public async Task Should_be_able_to_get_all_actor_states_from_ActorService()
		{
			var result = await ExecutionHelper.ExecuteWithRetriesAsync((ct) =>
			{
				return _proxy.GetCountsAsync(CancellationToken.None);
			}, 3, TimeSpan.FromMilliseconds(3), CancellationToken.None);

			result.Should().BeEquivalentTo(new[] {1, 2, 3});
		}

		[Test]
		public async Task Should_be_able_to_remove_actor_from_ActorService()
		{

			await ExecutionHelper.ExecuteWithRetriesAsync((ct) =>
			{
				_proxy.RemoveAsync(new ActorId("first"), CancellationToken.None);
				return Task.FromResult(true);

			}, 3, TimeSpan.FromMilliseconds(3), CancellationToken.None);

			var result = await ExecutionHelper.ExecuteWithRetriesAsync((ct) =>
			{
				return _proxy.GetCountsAsync(CancellationToken.None);
			}, 3, TimeSpan.FromMilliseconds(3), CancellationToken.None);

			result.Should().BeEquivalentTo(new[] {2, 3});
		}		
	}
}