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
			StateSessionManagerBase<TextStateSessionManagerWithTransaction.TextStateSession>.StateSessionBase<
				TextStateSessionManagerWithTransaction>, IStateSession
		{
			private readonly object _lock = new object();
			private readonly TextStateSessionManagerWithTransaction _manager;

			protected TextStateSession(
				TextStateSessionManagerWithTransaction manager,
				bool readOnly,
				IStateSessionObject[] stateSessionObjects)
				: base(manager, readOnly, stateSessionObjects)
			{
				_manager = manager;
			}

			private IStateSessionManagerInternals _managerInternals => _manager;

			private bool ContainsByRead(string id)
			{
				return Read(id, checkExistsOnly: true) != null;
			}

			protected abstract string Read(string id, bool checkExistsOnly = false);

			protected abstract void Delete(string id);

			protected abstract void Write(string id, string content);

			protected abstract FindByKeyPrefixResult Find(string idPrefix, string key, int maxNumResults = 100000,
				ContinuationToken continuationToken = null,
				CancellationToken cancellationToken = new CancellationToken());

			protected override Task<bool> ContainsInternal(SchemaStateKey id,
				CancellationToken cancellationToken = new CancellationToken())
			{
				try
				{
					lock (_lock)
					{
						return Task.FromResult(ContainsByRead(id));
					}
				}
				catch (Exception ex)
				{
					throw new StateSessionException($"ContainsInternal for {id} failed", ex);
				}
			}

			protected override Task<StateWrapper<T>> GetValueInternalAsync<T>(string id,
				CancellationToken cancellationToken = new CancellationToken())
			{
				try
				{
					StateWrapper<T> value = null;
					lock (_lock)
					{
						var stringValue = Read(id);
						if (stringValue == null)
						{
							throw new KeyNotFoundException($"State with {id} does not exist");
						}

						value = Newtonsoft.Json.JsonConvert.DeserializeObject<StateWrapper<T>>(stringValue,
							new JsonSerializerSettings {TypeNameHandling = TypeNameHandling.Auto});
					}
					return Task.FromResult(value);
				}
				catch (Exception ex)
				{
					throw new StateSessionException($"TryGetValueAsync for {id} failed, {ex.Message}", ex);
				}
			}

			protected override Task<ConditionalValue<StateWrapper<T>>> TryGetValueInternalAsync<T>(SchemaStateKey id,
				CancellationToken cancellationToken = new CancellationToken())
			{
				try
				{
					StateWrapper<T> value;
					lock (_lock)
					{
						var stringValue = Read(id);
						if (stringValue == null)
						{
							return Task.FromResult(new ConditionalValue<StateWrapper<T>>(false, null));
						}

						value = Newtonsoft.Json.JsonConvert.DeserializeObject<StateWrapper<T>>(stringValue,
							new JsonSerializerSettings {TypeNameHandling = TypeNameHandling.Auto});
					}
					return Task.FromResult(new ConditionalValue<StateWrapper<T>>(true, value));
				}
				catch (Exception ex)
				{
					throw new StateSessionException($"TryGetValueAsync for {id} failed", ex);
				}
			}

			protected override Task SetValueInternalAsync(SchemaStateKey id, StateWrapper value,
				Type valueType, CancellationToken cancellationToken = new CancellationToken())
			{
				try
				{
					lock (_lock)
					{
						if (value == null)
						{
							if (ContainsByRead(id))
							{
								Delete(id);
							}
						}
						else
						{
							var stringValue = JsonConvert.SerializeObject(value,
								new JsonSerializerSettings() {TypeNameHandling = TypeNameHandling.Auto, Formatting = Formatting.Indented});
							Write(id, stringValue);
						}
					}

					return Task.FromResult(true);
				}
				catch (Exception ex)
				{
					throw new StateSessionException($"SetValueAsync for {id} failed", ex);
				}
			}

			protected override Task RemoveInternalAsync(SchemaStateKey id,
				CancellationToken cancellationToken = new CancellationToken())
			{
				try
				{
					lock (_lock)
					{
						if (ContainsByRead(id))
						{
							Delete(id);
						}
					}

					return Task.FromResult(true);
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
					Task.FromResult(new FindByKeyPrefixResult()
					{
						ContinuationToken = result.ContinuationToken,
						Items = result.Items.Select(i => _managerInternals.GetUnescapedKey(i.Substring(schemaKeyPrefix.Length))).ToArray()
					});
			}

			protected override Task<IEnumerable<string>> EnumerateSchemaNamesInternalAsync(string schemaKeyPrefix, string key,
				CancellationToken cancellationToken = new CancellationToken())
			{
				return Task.FromResult((IEnumerable<string>)
					Find(schemaKeyPrefix, key, int.MaxValue, null, cancellationToken)
						.Items
						.Select(id => id.Substring(schemaKeyPrefix.Length, id.Length - schemaKeyPrefix.Length - key.Length - 1))
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