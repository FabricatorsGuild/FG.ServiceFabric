using System;
using System.Collections.Generic;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using FG.ServiceFabric.DocumentDb;
using FG.ServiceFabric.DocumentDb.CosmosDb;
using FG.ServiceFabric.Services.Runtime.StateSession;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Query;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Data;

namespace FG.ServiceFabric.Actors.Runtime
{
	public class StateSessionActorStateProvider : IActorStateProvider
	{
		private IActorStateProvider _actorStateProvider;
		private readonly ServiceContext _context;
		private Guid _partitionId;
		private string _partitionKey;
		private readonly IStateSession _stateSession;

		private const string ActorIdStateSchemaName = @"actorId";
		private const string ActorStateSchemaName = @"actorState";
		private const string ActorReminderSchemaName = @"actorReminder";

		protected StateSessionActorStateProvider(ServiceContext context, IStateSession stateSession, ActorTypeInformation actorTypeInfo)
		{
			if (actorTypeInfo != null)
			{
				_actorStateProvider = ActorStateProviderHelper.CreateDefaultStateProvider(actorTypeInfo);
			}
			_context = context;
			_stateSession = stateSession;

			_partitionId = _context.PartitionId;
			_partitionKey = StateSessionHelper.GetPartitionInfo(_context).GetAwaiter().GetResult();
		}

		private string GetActorStateKey(ActorId actorId, string stateName)
		{
			return StateSessionHelper.GetActorStateName(actorId, stateName);
		}

		private string GetActorStateKeyPrefix(ActorId actorId)
		{
			return StateSessionHelper.GetActorStateNamePrefix(actorId);
		}

		private string GetActorIdStateKey(ActorId actorId)
		{
			return StateSessionHelper.GetActorIdStateName(actorId);
		}

		private string GetActorIdStateKeyPrefix()
		{
			return StateSessionHelper.GetActorIdStateNamePrefix();
		}

		private string GetActorStateName(ActorId actorId, string actorStateName)
		{
			return $"{actorId}_{actorStateName}";
		}
		private string GetActorIdStateName(ActorId actorId)
		{
			return actorId.ToString();
		}

		private string GetActorReminderStateName(ActorId actorId, string reminderName)
		{
			return $"{actorId}_{reminderName}";
		}

		#region Replication
		public void Initialize(StatefulServiceInitializationParameters initializationParameters)
		{
			throw new NotImplementedException();
		}

		public Task<IReplicator> OpenAsync(ReplicaOpenMode openMode, IStatefulServicePartition partition, CancellationToken cancellationToken)
		{
			return _actorStateProvider.OpenAsync(openMode, partition, cancellationToken);
		}

		public Task ChangeRoleAsync(ReplicaRole newRole, CancellationToken cancellationToken)
		{
			return _actorStateProvider.ChangeRoleAsync(newRole, cancellationToken);
		}

		public Task CloseAsync(CancellationToken cancellationToken)
		{
			return _actorStateProvider.CloseAsync(cancellationToken);
		}

		public Task BackupAsync(Func<BackupInfo, CancellationToken, Task<bool>> backupCallback)
		{
			return Task.FromResult(true);
		}

		public Task BackupAsync(BackupOption option, TimeSpan timeout, CancellationToken cancellationToken, Func<BackupInfo, CancellationToken, Task<bool>> backupCallback)
		{
			return Task.FromResult(true);
		}

		public Task RestoreAsync(string backupFolderPath)
		{
			return Task.FromResult(true);
		}

		public Task RestoreAsync(string backupFolderPath, RestorePolicy restorePolicy, CancellationToken cancellationToken)
		{
			return Task.FromResult(true);
		}

		public Func<CancellationToken, Task<bool>> OnDataLossAsync { get; set; }

		public void Initialize(ActorTypeInformation actorTypeInformation)
		{
			if (actorTypeInformation != null)
			{
				_actorStateProvider = ActorStateProviderHelper.CreateDefaultStateProvider(actorTypeInformation);
			}

		}

		#endregion

		public void Abort()
		{
		}

		public async Task ActorActivatedAsync(ActorId actorId, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;

			var stateName = actorId.ToString();

			var existingActor = await _stateSession.TryGetValueAsync<StateWrapper<ExternalActorIdState>>(ActorIdStateSchemaName, stateName, cancellationToken);
			if (!existingActor.HasValue)
			{
				var actorIdState = new ExternalActorIdState(actorId);
				await _stateSession.SetValueAsync(ActorIdStateSchemaName, stateName, actorIdState, cancellationToken);
			}
		}

		public async Task ReminderCallbackCompletedAsync(ActorId actorId, IActorReminder reminder, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;

			var stateName = GetActorReminderStateName(actorId, reminder.Name);

			var reminderCompletedState = new ActorReminderCompletedData(actorId, reminder.Name, DateTime.UtcNow);
			await _stateSession.SetValueAsync(ActorReminderSchemaName, stateName, reminderCompletedState, cancellationToken);			
		}

		public async Task<T> LoadStateAsync<T>(ActorId actorId, string actorStateName, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;

			var stateName = GetActorStateName(actorId, actorStateName);

			var stateWrapper = await _stateSession.GetValueAsync<StateWrapper<T>>(ActorStateSchemaName, stateName, cancellationToken);
			return stateWrapper.State;
		}

		public async Task SaveStateAsync(ActorId actorId, IReadOnlyCollection<ActorStateChange> stateChanges, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;

			foreach (var actorStateChange in stateChanges)
			{
				var actorStateName = actorStateChange.StateName;
				var stateName = GetActorStateName(actorId, actorStateName);

				switch (actorStateChange.ChangeKind)
				{
					case (StateChangeKind.Add):
					case (StateChangeKind.Update):

						await _stateSession.SetValueAsync(ActorStateSchemaName, stateName, actorStateChange.Type, actorStateChange.Value, cancellationToken);

						break;
					case (StateChangeKind.Remove):
						break;
					case (StateChangeKind.None):
						break;
				}

			}
		}

		public Task<bool> ContainsStateAsync(ActorId actorId, string stateName, CancellationToken cancellationToken = new CancellationToken())
		{
			throw new NotImplementedException();
		}

		public Task RemoveActorAsync(ActorId actorId, CancellationToken cancellationToken = new CancellationToken())
		{
			throw new NotImplementedException();
		}

		public Task<IEnumerable<string>> EnumerateStateNamesAsync(ActorId actorId, CancellationToken cancellationToken = new CancellationToken())
		{
			throw new NotImplementedException();
		}

		public Task<PagedResult<ActorId>> GetActorsAsync(int numItemsToReturn, ContinuationToken continuationToken, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}


		public Task SaveReminderAsync(ActorId actorId, IActorReminder reminder, CancellationToken cancellationToken = new CancellationToken())
		{
			throw new NotImplementedException();
		}

		public Task DeleteReminderAsync(ActorId actorId, string reminderName, CancellationToken cancellationToken = new CancellationToken())
		{
			throw new NotImplementedException();
		}

		public Task DeleteRemindersAsync(IReadOnlyDictionary<ActorId, IReadOnlyCollection<string>> reminderNames, CancellationToken cancellationToken = new CancellationToken())
		{
			throw new NotImplementedException();
		}

		public Task<IActorReminderCollection> LoadRemindersAsync(CancellationToken cancellationToken = new CancellationToken())
		{
			throw new NotImplementedException();
		}
	}
}