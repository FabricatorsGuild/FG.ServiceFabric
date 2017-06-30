using System;
using System.Threading;
using System.Threading.Tasks;
using FG.Common.Async;
using FG.Common.Utils;
using FG.ServiceFabric.CQRS;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Actors.Runtime
{
    public static class CommandExecutionHelper
    {
        private const string VoidReturnValue = "void";

        public static async Task<T> ExecuteCommandAsync<T>
            (Func<CancellationToken, Task<T>> func, ICommand command, IActorStateManager stateManager, CancellationToken cancellationToken)
            where T : struct
        {
            T? returnValue = null;
            await ExecutionHelper.ExecuteWithRetriesAsync(
                async ct =>
                {
                    var conditionalValue = await stateManager.TryGetStateAsync<object>(GetStateKey(command), ct);
                    
                    if(conditionalValue.HasValue)
                    {
                        returnValue = (T)conditionalValue.Value;
                    }
                },
                maxRetries: 3,
                retryDelay: 4.Seconds(),
                userCancellationToken: CancellationToken.None);

            if(returnValue != null) return await Task.FromResult(returnValue.Value);

            returnValue = await func(cancellationToken);

            await StoreCommand(command, stateManager, returnValue);

            return returnValue ?? default(T);
        }
        private static string GetStateKey(ICommand command) { return $"command{command.CommandId}"; }

        public static async Task ExecuteCommandAsync
            (Func<CancellationToken, Task> func, ICommand command, IActorStateManager stateManager, CancellationToken cancellationToken)
        {
            if (await HasPreviousExecution(command, stateManager))
            {
                return;
            }

            await func(cancellationToken);

            await StoreCommand(command, stateManager, true);
        }

        public static async Task ExecuteCommandAsync
            (Action<CancellationToken> action, ICommand command, IActorStateManager stateManager, CancellationToken cancellationToken)
        {
            if(await HasPreviousExecution(command, stateManager))
            {
                return;
            }

            action(cancellationToken);
            
            await StoreCommand(command, stateManager, VoidReturnValue);
        }

        private static async Task<bool> HasPreviousExecution(ICommand command, IActorStateManager stateManager)
        {
            var hasPreviousExecution = false;
            await ExecutionHelper.ExecuteWithRetriesAsync(
                async ct => { hasPreviousExecution = await stateManager.ContainsStateAsync(GetStateKey(command), ct); },
                maxRetries: 3,
                retryDelay: 4.Seconds(),
                userCancellationToken: CancellationToken.None);

            return hasPreviousExecution;
        }

        private static async Task StoreCommand(ICommand command, IActorStateManager stateManager, object returnValue)
        {
            await ExecutionHelper.ExecuteWithRetriesAsync(
                async ct => { await stateManager.AddStateAsync(GetStateKey(command), returnValue, ct); },
                maxRetries: 3,
                retryDelay: 4.Seconds(),
                userCancellationToken: CancellationToken.None);
        }
    }
}