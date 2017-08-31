using System;
using System.Collections.Generic;
using System.Fabric;
using System.Threading.Tasks;
using FG.ServiceFabric.Actors.Runtime;
using FG.ServiceFabric.Services.Runtime.State;

namespace FG.ServiceFabric.Services.Runtime
{
	public class DocumentStorageStatefulServiceStateManagerSession : WrappedStatefulServiceStateManagerSession
	{
		private readonly IDocumentStorageSession _documentStorageSession;

		private readonly string _partitionId;
		private readonly string _serviceName;

		private string GetSchemaStateKey(string schema, string stateName)
		{
			var stateKey = $"{GetSchemaStateKeyPrefix(schema)}{stateName}";
			return stateKey;
		}

		private string GetSchemaStateKeyPrefix(string schema)
		{
			var stateKeyPrefix = $"@@{_serviceName}_{_partitionId}_{schema}_";
			return stateKeyPrefix;
		}

		private string GetSchemaQueueStateHeadKey(string schema)
		{
			var stateKey = $"{GetSchemaStateKeyPrefix(schema)}_queue_head";
			return stateKey;
		}
		private string GetSchemaQueueStateTailKey(string schema)
		{
			var stateKey = $"{GetSchemaStateKeyPrefix(schema)}_queue_tail";
			return stateKey;
		}
		private string GetSchemaQueueStateKey(string schema, long index)
		{
			var stateKey = $"{GetSchemaStateKeyPrefix(schema)}_{index}";
			return stateKey;
		}

		public DocumentStorageStatefulServiceStateManagerSession(
			StatefulServiceContext serviceContext,
			IStatefulServiceStateManagerSession innerSession,
			IDocumentStorageSession documentStorageSession) : base(innerSession)
		{
			_documentStorageSession = documentStorageSession;
			innerSession.SessionCommitted += InnerSessionOnSessionCommitted;

			_partitionId = serviceContext.PartitionId.ToString();
			_serviceName = serviceContext.ServiceTypeName;
		}

		public override async Task<T> GetOrAddAsync<T>(string schema, string storageKey, Func<string, T> newValue)
		{
			var internalValue = await base.TryGetAsync<T>(schema, storageKey);
			if (internalValue.HasValue)
			{
				return internalValue.Value;
			}

			try
			{
				// Load from database?
				var stateKey = GetSchemaStateKey(schema, storageKey);
				var containsExternalContainsState = await _documentStorageSession.ContainsAsync(stateKey);

				if (containsExternalContainsState)
				{
					var externalState = await _documentStorageSession.ReadAsync(stateKey);
					var externalStateObject = externalState.Value;

					await base.SetAsync(schema, storageKey, externalStateObject);

					return (T) externalStateObject;
				}
				else
				{
					// Add to both internal and external
					await SetAsync(schema, storageKey, newValue(storageKey));
				}
			}
			catch (Exception ex)
			{
				// Log this
				// TODO: Add this to a list of unsynced changes???
				throw new KeyNotFoundException($"State with name {schema}:{storageKey} was not found", ex);
			}
			throw new KeyNotFoundException($"State with name {schema}:{storageKey} was not found");

		}

		public override async Task SetAsync<T>(string schema, string storageKey, T value)
		{
			await base.SetAsync(schema, storageKey, value);

			var stateKey = GetSchemaStateKey(schema, storageKey);

			var externalState = new ExternalState()
			{
				Key = stateKey,
				Value = value,
			};
			await _documentStorageSession.UpsertAsync(stateKey, externalState);

			await base.SetAsync(schema, storageKey, value);
		}

		private async Task PersistStateChanges(IEnumerable<ReliableStateChange> stateChanges)
		{
			foreach (var stateChange in stateChanges)
			{
				string stateKey = null;
				var index = 0L;
				switch (stateChange.ChangeKind)
				{
					case (ReliableStateChangeKind.Add):
					case (ReliableStateChangeKind.AddOrUpdate):
					case (ReliableStateChangeKind.Update):
						stateKey = GetSchemaStateKey(stateChange.Schema, stateChange.StateName);
						ExternalState externalState = null;
						if (stateChange.ChangeKind != ReliableStateChangeKind.Dequeue && stateChange.ChangeKind != ReliableStateChangeKind.Remove)
						{
							externalState = new ExternalState()
							{
								Key = stateKey,
								Value = stateChange.Value,
							};
						}

						await _documentStorageSession.UpsertAsync(stateKey, externalState);

						break;
					case (ReliableStateChangeKind.Remove):

						stateKey = GetSchemaStateKey(stateChange.Schema, stateChange.StateName);

						await _documentStorageSession.DeleteAsync(stateKey);

						break;
					case (ReliableStateChangeKind.Enqueue):

						var stateKeyHead = GetSchemaQueueStateHeadKey(stateChange.Schema);
						var stateHead = await _documentStorageSession.ReadAsync(stateKeyHead);
						var head = long.Parse(stateHead?.Value?.ToString() ?? "0");

						head++;
						stateKey = GetSchemaQueueStateKey(stateChange.Schema, head);

						externalState = new ExternalState()
						{
							Key = stateKey,
							Value = stateChange.Value,
						};

						await _documentStorageSession.UpsertAsync(stateKey, externalState);
						await _documentStorageSession.UpsertAsync(stateKeyHead, new ExternalState(){Key = stateKeyHead, Value = head});

						break;
					case (ReliableStateChangeKind.Dequeue):
						var stateKeyTail = GetSchemaQueueStateTailKey(stateChange.Schema);
						var stateTail = await _documentStorageSession.ReadAsync(stateKeyTail);
						var tail = long.Parse(stateTail?.Value?.ToString() ?? "0");

						index = tail;
						stateKey = GetSchemaQueueStateKey(stateChange.Schema, index);
						tail = index + 1;

						await _documentStorageSession.DeleteAsync(stateKey);
						await _documentStorageSession.UpsertAsync(stateKeyTail, new ExternalState() { Key = stateKeyTail, Value = tail });

						break;
				}
			}
		}

		private async void InnerSessionOnSessionCommitted(object sender, SessionCommittedEventArgs sessionCommittedEventArgs)
		{
			await PersistStateChanges(sessionCommittedEventArgs.StateChanges);
		}
	}
}