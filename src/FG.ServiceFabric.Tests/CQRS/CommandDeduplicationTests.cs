using System;
using System.Threading;
using System.Threading.Tasks;
using FG.Common.Async;
using FG.Common.Utils;
using FG.CQRS;
using FG.ServiceFabric.Actors.Runtime;
using FG.ServiceFabric.Testing.Mocks;
using FG.ServiceFabric.Testing.Mocks.Services.Runtime;
using FluentAssertions;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using NUnit.Framework;
using ActorBase = FG.ServiceFabric.Actors.Runtime.ActorBase;
using ActorService = Microsoft.ServiceFabric.Actors.Runtime.ActorService;

namespace FG.ServiceFabric.Tests.CQRS
{
    [TestFixture]
    public class IdempotentCommandHandlingTests
    {
        [SetUp]
        public void Setup()
        {
            _fabricRuntime = new MockFabricRuntime();
            _application = _fabricRuntime.RegisterApplication("Overlord");
            _application.SetupActor(
                (service, actorId) => new TestActor(service, actorId),
                serviceDefinition: MockServiceDefinition.CreateUniformInt64Partitions(1));
            _command = new AddCountCommand();
        }

        private MockFabricApplication _application;
        private MockFabricRuntime _fabricRuntime;
        private AddCountCommand _command;

        [Test]
        public async Task Can_execute_command()
        {
            var id = new ActorId(Guid.NewGuid().ToString());
            await _fabricRuntime.ActorProxyFactory
                .CreateActorProxy<ITestActor>(id)
                .AddCountAsyncPerformingSyncAction(_command);
            var count = await _fabricRuntime.ActorProxyFactory
                .CreateActorProxy<ITestActor>(id)
                .GetCountAsync();
            count.Should().Be(1);
        }

        [Test]
        public async Task
            Executing_same_command_adding_count_twice_idempotently_performing_async_func_are_returning_same_value_as_in_first_execution()
        {
            var id = new ActorId(Guid.NewGuid().ToString());
            var returnValue1 = await _fabricRuntime.ActorProxyFactory
                .CreateActorProxy<ITestActor>(id)
                .AddCountAsyncPerformingAsyncFuncReturningValue(_command);
            var returnValue2 = await _fabricRuntime.ActorProxyFactory
                .CreateActorProxy<ITestActor>(id)
                .AddCountAsyncPerformingAsyncFuncReturningValue(
                    _command); //second time, note its same command, hence same commandid.

            returnValue2.Should().Be(returnValue1);
        }

        [Test]
        public async Task
            Executing_same_command_adding_count_twice_idempotently_performing_async_func_results_in_count_being_1()
        {
            var id = new ActorId(Guid.NewGuid().ToString());
            await _fabricRuntime.ActorProxyFactory
                .CreateActorProxy<ITestActor>(id)
                .AddCountAsyncPerformingAsyncFunc(_command);
            await _fabricRuntime.ActorProxyFactory
                .CreateActorProxy<ITestActor>(id)
                .AddCountAsyncPerformingAsyncFunc(_command); //second time, note its same command, hence same commandid.
            var count = await _fabricRuntime.ActorProxyFactory
                .CreateActorProxy<ITestActor>(id)
                .GetCountAsync();
            count.Should().Be(1);
        }

        [Test]
        public async Task
            Executing_same_command_adding_count_twice_idempotently_performing_sync_action_results_in_count_being_1()
        {
            var id = new ActorId(Guid.NewGuid().ToString());
            await _fabricRuntime.ActorProxyFactory
                .CreateActorProxy<ITestActor>(id)
                .AddCountAsyncPerformingSyncAction(_command);
            await _fabricRuntime.ActorProxyFactory
                .CreateActorProxy<ITestActor>(id)
                .AddCountAsyncPerformingSyncAction(
                    _command); //second time, note its same command, hence same commandid.
            var count = await _fabricRuntime.ActorProxyFactory
                .CreateActorProxy<ITestActor>(id)
                .GetCountAsync();
            count.Should().Be(1);
        }

        [Test]
        public async Task Executing_same_command_adding_count_twice_non_idempotently_results_in_count_being_2()
        {
            var id = new ActorId(Guid.NewGuid().ToString());
            await _fabricRuntime.ActorProxyFactory
                .CreateActorProxy<ITestActor>(id)
                .AddCountAsyncNonIdempotently(_command);
            await _fabricRuntime.ActorProxyFactory
                .CreateActorProxy<ITestActor>(id)
                .AddCountAsyncNonIdempotently(_command); //second time, note its same command, hence same commandid.
            var count = await _fabricRuntime.ActorProxyFactory
                .CreateActorProxy<ITestActor>(id)
                .GetCountAsync();
            count.Should().Be(2);
        }
    }

    public interface ITestActor : IActor
    {
        Task<int> AddCountAsyncPerformingAsyncFuncReturningValue(AddCountCommand command);
        Task AddCountAsyncPerformingAsyncFunc(AddCountCommand command);
        Task AddCountAsyncPerformingSyncAction(AddCountCommand command);
        Task AddCountAsyncPerformingSyncActionThrowingException(AddCountCommand command);
        Task AddCountAsyncNonIdempotently(AddCountCommand command);
        Task<int> GetCountAsync();
    }

    public class AddCountCommand : CommandBase
    {
    }

    [StatePersistence(StatePersistence.Persisted)]
    internal class TestActor :
        ActorBase,
        ITestActor
    {
        public TestActor(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
        }

        public async Task<int> AddCountAsyncPerformingAsyncFuncReturningValue(AddCountCommand command)
        {
            return await CommandDeduplicationHelper.ProcessOnceAsync(
                async token =>
                {
                    await ExecutionHelper.ExecuteWithRetriesAsync(
                        async ct => { await StateManager.AddOrUpdateStateAsync("MyState", 1, (s, i) => ++i, ct); },
                        3,
                        1.Seconds(),
                        CancellationToken.None);
                    return await GetCountAsync();
                },
                command,
                StateManager,
                CancellationToken.None);
        }

        public async Task AddCountAsyncPerformingAsyncFunc(AddCountCommand command)
        {
            await CommandDeduplicationHelper.ProcessOnceAsync(
                async token =>
                {
                    await ExecutionHelper.ExecuteWithRetriesAsync(
                        async ct => { await StateManager.AddOrUpdateStateAsync("MyState", 1, (s, i) => ++i, ct); },
                        3,
                        1.Seconds(),
                        CancellationToken.None);
                },
                command,
                StateManager,
                CancellationToken.None);
        }

        public async Task AddCountAsyncPerformingSyncAction(AddCountCommand command)
        {
            await CommandDeduplicationHelper.ProcessOnceAsync(
                () =>
                {
                    ExecutionHelper.ExecuteWithRetriesAsync(
                        async ct => { await StateManager.AddOrUpdateStateAsync("MyState", 1, (s, i) => ++i, ct); },
                        3,
                        1.Seconds(),
                        CancellationToken.None).GetAwaiter().GetResult();
                },
                command,
                StateManager,
                CancellationToken.None);
        }

        public async Task AddCountAsyncPerformingSyncActionThrowingException(AddCountCommand command)
        {
            await CommandDeduplicationHelper.ProcessOnceAsync(
                () => { throw new Exception("Catch this."); },
                command,
                StateManager,
                CancellationToken.None);
        }

        public async Task AddCountAsyncNonIdempotently(AddCountCommand command)
        {
            await ExecutionHelper.ExecuteWithRetriesAsync(
                async ct => { await StateManager.AddOrUpdateStateAsync("MyState", 1, (s, i) => ++i, ct); },
                3,
                1.Seconds(),
                CancellationToken.None);
        }

        public async Task<int> GetCountAsync()
        {
            return await StateManager.GetStateAsync<int>("MyState");
        }
    }
}