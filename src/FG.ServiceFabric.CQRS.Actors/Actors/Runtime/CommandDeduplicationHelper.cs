using System;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using FG.Common.Async;
using FG.Common.Utils;
using FG.CQRS;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Actors.Runtime
{
    [DataContract]
    internal sealed class CommandReturnValue
    {
        [DataMember]
        public object ReturnValue { get; set; }

        [IgnoreDataMember]
        public bool HasReturnValue => ReturnValue != null;

        private CommandReturnValue()
        {
        }

        private CommandReturnValue(object returnValue)
        {
            ReturnValue = returnValue;
        }

        public static CommandReturnValue None()
        {
            return new CommandReturnValue();
        }

        public static CommandReturnValue Create(object returnValue)
        {
            return new CommandReturnValue(returnValue);
        }
    }

    /// <summary>
    /// Message deduplication helper can be used when it is not possible to achieve natural idempotency.
    /// </summary>
    public static class CommandDeduplicationHelper
    {
        private const string CommandStateKeyPrefix = "fg__command_";

        public static async Task<TReturnValue> ProcessOnceAsync<TReturnValue>
        (Func<CancellationToken, Task<TReturnValue>> func, ICommand command, IActorStateManager stateManager,
            CancellationToken cancellationToken)
            where TReturnValue : struct
        {
            if (await HasPreviousExecution(command, stateManager, cancellationToken))
            {
                CommandReturnValue storedReturnValue = null;
                await ExecutionHelper.ExecuteWithRetriesAsync(
                    async ct =>
                    {
                        var conditionalValue =
                            await stateManager.TryGetStateAsync<CommandReturnValue>(GetStateKey(command), ct);

                        if (conditionalValue.HasValue && conditionalValue.Value.HasReturnValue)
                        {
                            storedReturnValue = conditionalValue.Value;
                        }
                    },
                    maxRetries: 3,
                    retryDelay: 1.Seconds(),
                    userCancellationToken: cancellationToken);

                if (storedReturnValue.HasReturnValue)
                {
                    return (TReturnValue) storedReturnValue.ReturnValue;
                }

                return default(TReturnValue);
            }

            var returnValue = await func(cancellationToken);
            await StoreCommandAndReturnValue(command, stateManager, CommandReturnValue.Create(returnValue),
                cancellationToken);
            return returnValue;
        }

        public static async Task ProcessOnceAsync
        (Func<CancellationToken, Task> func, ICommand command, IActorStateManager stateManager,
            CancellationToken cancellationToken)
        {
            if (await HasPreviousExecution(command, stateManager, cancellationToken))
            {
                return;
            }

            await func(cancellationToken);
            await StoreCommand(command, stateManager, cancellationToken);
        }

        public static async Task ProcessOnceAsync
            (Action action, ICommand command, IActorStateManager stateManager, CancellationToken cancellationToken)
        {
            if (await HasPreviousExecution(command, stateManager, cancellationToken))
            {
                return;
            }

            action();
            await StoreCommand(command, stateManager, cancellationToken);
        }

        private static async Task<bool> HasPreviousExecution(ICommand command, IActorStateManager stateManager,
            CancellationToken cancellationToken)
        {
            return await ExecutionHelper.ExecuteWithRetriesAsync(
                ct => stateManager.ContainsStateAsync(GetStateKey(command), ct),
                maxRetries: 3,
                retryDelay: 1.Seconds(),
                userCancellationToken: cancellationToken);
        }

        private static Task StoreCommand(ICommand command, IActorStateManager stateManager,
            CancellationToken cancellationToken)
        {
            return StoreCommandAndReturnValue(command, stateManager, CommandReturnValue.None(), cancellationToken);
        }

        private static Task StoreCommandAndReturnValue(ICommand command, IActorStateManager stateManager,
            CommandReturnValue returnValue, CancellationToken cancellationToken)
        {
            return ExecutionHelper.ExecuteWithRetriesAsync(
                async ct => { await stateManager.AddStateAsync(GetStateKey(command), returnValue, ct); },
                maxRetries: 3,
                retryDelay: 1.Seconds(),
                userCancellationToken: cancellationToken);
        }

        private static string GetStateKey(ICommand command)
        {
            return $"{CommandStateKeyPrefix}{command.CommandId}";
        }
    }
}