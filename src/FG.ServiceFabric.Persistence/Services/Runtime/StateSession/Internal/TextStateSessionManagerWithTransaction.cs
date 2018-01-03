using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors.Query;
using Microsoft.ServiceFabric.Data;
using Newtonsoft.Json;

namespace FG.ServiceFabric.Services.Runtime.StateSession.Internal
{
    public abstract class TextStateSessionManagerWithTransaction :
        StateSessionManagerBase<TextStateSessionManagerWithTransaction.TextStateSession>, IStateSessionManager
    {
        protected TextStateSessionManagerWithTransaction(
            string serviceName,
            Guid partitionId,
            string partitionKey) :
            base(serviceName, partitionId, partitionKey)
        {
        }

        public abstract class TextStateSession :
            StateSessionBase<
                TextStateSessionManagerWithTransaction>, IStateSession
        {
            private readonly object _lock = new object();
            private readonly TextStateSessionManagerWithTransaction _manager;

            protected TextStateSession(
                TextStateSessionManagerWithTransaction manager,
                IStateSessionObject[] stateSessionObjects)
                : base(manager, stateSessionObjects)
            {
                _manager = manager;
            }

            protected TextStateSession(
                TextStateSessionManagerWithTransaction manager,
                IStateSessionReadOnlyObject[] stateSessionObjects)
                : base(manager, stateSessionObjects)
            {
                _manager = manager;
            }

            private IStateSessionManagerInternals _managerInternals => _manager;

            private async Task<bool> ContainsByReadAsync(string id)
            {
                var value = await ReadAsync(id, true);
                return value != null;
            }

            protected abstract Task<string> ReadAsync(string id, bool checkExistsOnly = false);

            protected abstract Task DeleteAsync(string id);

            protected abstract Task WriteAsync(string id, string content);

            protected abstract FindByKeyPrefixResult Find(string idPrefix, string key, int maxNumResults = 100000,
                ContinuationToken continuationToken = null,
                CancellationToken cancellationToken = new CancellationToken());

            protected override Task<bool> ContainsInternal(SchemaStateKey id,
                CancellationToken cancellationToken = new CancellationToken())
            {
                try
                {
                    return ContainsByReadAsync(id);
                }
                catch (Exception ex)
                {
                    throw new StateSessionException($"ContainsInternal for {id} failed", ex);
                }
            }

            protected override async Task<StateWrapper<T>> GetValueInternalAsync<T>(string id,
                CancellationToken cancellationToken = new CancellationToken())
            {
                try
                {
                    StateWrapper<T> value = null;
                    var stringValue = await ReadAsync(id);
                    if (stringValue == null)
                        throw new KeyNotFoundException($"State with {id} does not exist");

                    value = JsonConvert.DeserializeObject<StateWrapper<T>>(stringValue,
                        new JsonSerializerSettings {TypeNameHandling = TypeNameHandling.Auto});
                    return value;
                }
                catch (Exception ex)
                {
                    throw new StateSessionException($"TryGetValueAsync for {id} failed, {ex.Message}", ex);
                }
            }

            protected override async Task<ConditionalValue<StateWrapper<T>>> TryGetValueInternalAsync<T>(
                SchemaStateKey id,
                CancellationToken cancellationToken = new CancellationToken())
            {
                try
                {
                    var stringValue = await ReadAsync(id);
                    if (stringValue == null)
                        return new ConditionalValue<StateWrapper<T>>(false, null);

                    var value = JsonConvert.DeserializeObject<StateWrapper<T>>(stringValue,
                        new JsonSerializerSettings {TypeNameHandling = TypeNameHandling.Auto});
                    return new ConditionalValue<StateWrapper<T>>(true, value);
                }
                catch (Exception ex)
                {
                    throw new StateSessionException($"TryGetValueAsync for {id} failed", ex);
                }
            }

            protected override async Task SetValueInternalAsync(SchemaStateKey id, StateWrapper value,
                Type valueType, CancellationToken cancellationToken = new CancellationToken())
            {
                try
                {
                    if (value == null)
                    {
                        if (await ContainsByReadAsync(id))
                            await DeleteAsync(id);
                    }
                    else
                    {
                        var stringValue = JsonConvert.SerializeObject(value,
                            new JsonSerializerSettings
                            {
                                TypeNameHandling = TypeNameHandling.Auto,
                                Formatting = Formatting.Indented
                            });
                        await WriteAsync(id, stringValue);
                    }
                }
                catch (Exception ex)
                {
                    throw new StateSessionException($"SetValueAsync for {id} failed", ex);
                }
            }

            protected override async Task RemoveInternalAsync(SchemaStateKey id,
                CancellationToken cancellationToken = new CancellationToken())
            {
                try
                {
                    if (await ContainsByReadAsync(id))
                        await DeleteAsync(id);
                }
                catch (Exception ex)
                {
                    throw new StateSessionException($"RemoveAsync for {id} failed", ex);
                }
            }

            protected override Task<FindByKeyPrefixResult> FindByKeyPrefixInternalAsync(string schemaKeyPrefix,
                int maxNumResults = 100000, ContinuationToken continuationToken = null,
                CancellationToken cancellationToken = new CancellationToken())
            {
                var result = Find(schemaKeyPrefix, null, maxNumResults, continuationToken, cancellationToken);

                return
                    Task.FromResult(new FindByKeyPrefixResult
                    {
                        ContinuationToken = result.ContinuationToken,
                        Items = result.Items.Select(i =>
                            _managerInternals.GetUnescapedKey(i.Substring(schemaKeyPrefix.Length))).ToArray()
                    });
            }

            protected override Task<IEnumerable<string>> EnumerateSchemaNamesInternalAsync(string schemaKeyPrefix,
                string key,
                CancellationToken cancellationToken = new CancellationToken())
            {
                return Task.FromResult((IEnumerable<string>)
                    Find(schemaKeyPrefix, key, int.MaxValue, null, cancellationToken)
                        .Items
                        .Select(id => id.Substring(schemaKeyPrefix.Length,
                            id.Length - schemaKeyPrefix.Length - key.Length - 1))
                        .Distinct()
                        .ToArray());
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                }
            }
        }
    }
}