using System;
using System.Collections.Generic;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Query;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Data;

namespace FG.ServiceFabric.Actors.State
{
	public class VersionedStateProvider : IActorStateProvider
	{
		private const string StateVersionKey = @"version";
		private const string StateObjectKey = @"state";
		private readonly IActorStateProvider _innerStateProvider;
		private readonly IMigrationContainer _migrationContainer;


		public VersionedStateProvider(IActorStateProvider innerStateProvider, IMigrationContainer migrationContainer)
		{
			_innerStateProvider = innerStateProvider;
			_migrationContainer = migrationContainer;
		}

		public void Initialize(StatefulServiceInitializationParameters initializationParameters)
		{
			_innerStateProvider.Initialize(initializationParameters);
		}

		public Task<IReplicator> OpenAsync(ReplicaOpenMode openMode, IStatefulServicePartition partition,
			CancellationToken cancellationToken)
		{
			return _innerStateProvider.OpenAsync(openMode, partition, cancellationToken);
		}

		public Task ChangeRoleAsync(ReplicaRole newRole, CancellationToken cancellationToken)
		{
			return _innerStateProvider.ChangeRoleAsync(newRole, cancellationToken);
		}

		public Task CloseAsync(CancellationToken cancellationToken)
		{
			return _innerStateProvider.CloseAsync(cancellationToken);
		}

		public void Abort()
		{
			_innerStateProvider.Abort();
		}

		public Task BackupAsync(Func<BackupInfo, CancellationToken, Task<bool>> backupCallback)
		{
			return _innerStateProvider.BackupAsync(backupCallback);
		}

		public Task BackupAsync(BackupOption option, TimeSpan timeout, CancellationToken cancellationToken,
			Func<BackupInfo, CancellationToken, Task<bool>> backupCallback)
		{
			return _innerStateProvider.BackupAsync(option, timeout, cancellationToken, backupCallback);
		}

		public Task RestoreAsync(string backupFolderPath)
		{
			return _innerStateProvider.RestoreAsync(backupFolderPath);
		}

		public Task RestoreAsync(string backupFolderPath, RestorePolicy restorePolicy, CancellationToken cancellationToken)
		{
			return _innerStateProvider.RestoreAsync(backupFolderPath, restorePolicy, cancellationToken);
		}

		public Func<CancellationToken, Task<bool>> OnDataLossAsync { get; set; }

		public void Initialize(ActorTypeInformation actorTypeInformation)
		{
			_innerStateProvider.Initialize(actorTypeInformation);
		}

		public Task ActorActivatedAsync(ActorId actorId, CancellationToken cancellationToken = new CancellationToken())
		{
			return _innerStateProvider.ActorActivatedAsync(actorId, cancellationToken);
		}

		public Task ReminderCallbackCompletedAsync(ActorId actorId, IActorReminder reminder,
			CancellationToken cancellationToken = new CancellationToken())
		{
			return _innerStateProvider.ReminderCallbackCompletedAsync(actorId, reminder, cancellationToken);
		}

		public Task<T> LoadStateAsync<T>(ActorId actorId, string stateName,
			CancellationToken cancellationToken = new CancellationToken())
		{
			if (stateName == StateVersionKey)
				stateName = $"{stateName}_inner";
			if (stateName == StateObjectKey)
				_migrationContainer.EnsureUpdated(_innerStateProvider, actorId, StateObjectKey, StateVersionKey);
			return _innerStateProvider.LoadStateAsync<T>(actorId, stateName, cancellationToken);
		}

		public async Task SaveStateAsync(ActorId actorId, IReadOnlyCollection<ActorStateChange> stateChanges,
			CancellationToken cancellationToken = new CancellationToken())
		{
			var actorStateChanges = new List<ActorStateChange>();
			foreach (var stateChange in stateChanges)
			{
				if (stateChange.ChangeKind == StateChangeKind.Add)
				{
					var stateName = stateChange.StateName;
					if (stateName == StateVersionKey)
						stateName = $"{stateName}_inner";
					if (stateName == StateObjectKey)
					{
						actorStateChanges.Add(new ActorStateChange(stateName, stateChange.Type, stateChange.Value, StateChangeKind.Add));
						if ((await ContainsStateAsync(actorId, StateVersionKey, cancellationToken)))
						{
							actorStateChanges.Add(new ActorStateChange(StateVersionKey, typeof(int), _migrationContainer.CurrentState(),
								StateChangeKind.Update));
						}
						else
						{
							actorStateChanges.Add(new ActorStateChange(StateVersionKey, typeof(int), _migrationContainer.CurrentState(),
								StateChangeKind.Add));
						}
					}
				}
				else if (stateChange.ChangeKind == StateChangeKind.Update)
				{
					var stateName = stateChange.StateName;
					if (stateName == StateVersionKey)
						stateName = $"{stateName}_inner";
					if (stateName == StateObjectKey)
					{
						actorStateChanges.Add(
							new ActorStateChange(stateName, stateChange.Type, stateChange.Value, StateChangeKind.Update));
						if ((await ContainsStateAsync(actorId, StateVersionKey, cancellationToken)))
						{
							actorStateChanges.Add(new ActorStateChange(StateVersionKey, typeof(int), _migrationContainer.CurrentState(),
								StateChangeKind.Update));
						}
						else
						{
							actorStateChanges.Add(new ActorStateChange(StateVersionKey, typeof(int), _migrationContainer.CurrentState(),
								StateChangeKind.Add));
						}
					}
				}
				else if (stateChange.ChangeKind == StateChangeKind.Remove)
				{
					var stateName = stateChange.StateName;
					if (stateName == StateVersionKey)
						stateName = $"{stateName}_inner";
					if (stateName == StateObjectKey)
					{
						actorStateChanges.Add(
							new ActorStateChange(stateName, stateChange.Type, stateChange.Value, StateChangeKind.Remove));
						if ((await ContainsStateAsync(actorId, StateVersionKey, cancellationToken)))
						{
							actorStateChanges.Add(new ActorStateChange(StateVersionKey, typeof(int), _migrationContainer.CurrentState(),
								StateChangeKind.Remove));
						}
					}
				}
			}

			await _innerStateProvider.SaveStateAsync(actorId, actorStateChanges.AsReadOnly(), cancellationToken);
		}

		public Task<bool> ContainsStateAsync(ActorId actorId, string stateName,
			CancellationToken cancellationToken = new CancellationToken())
		{
			if (stateName == StateVersionKey)
				stateName = $"{stateName}_inner";
			return _innerStateProvider.ContainsStateAsync(actorId, stateName, cancellationToken);
		}

		public Task RemoveActorAsync(ActorId actorId, CancellationToken cancellationToken = new CancellationToken())
		{
			return _innerStateProvider.RemoveActorAsync(actorId, cancellationToken);
		}

		public Task<IEnumerable<string>> EnumerateStateNamesAsync(ActorId actorId,
			CancellationToken cancellationToken = new CancellationToken())
		{
			return _innerStateProvider.EnumerateStateNamesAsync(actorId, cancellationToken);
		}

		public Task<PagedResult<ActorId>> GetActorsAsync(int numItemsToReturn, ContinuationToken continuationToken,
			CancellationToken cancellationToken)
		{
			return _innerStateProvider.GetActorsAsync(numItemsToReturn, continuationToken, cancellationToken);
		}

		public Task SaveReminderAsync(ActorId actorId, IActorReminder reminder,
			CancellationToken cancellationToken = new CancellationToken())
		{
			return _innerStateProvider.SaveReminderAsync(actorId, reminder, cancellationToken);
		}

		public Task DeleteReminderAsync(ActorId actorId, string reminderName,
			CancellationToken cancellationToken = new CancellationToken())
		{
			return _innerStateProvider.DeleteReminderAsync(actorId, reminderName, cancellationToken);
		}

		public Task<IActorReminderCollection> LoadRemindersAsync(
			CancellationToken cancellationToken = new CancellationToken())
		{
			return _innerStateProvider.LoadRemindersAsync(cancellationToken);
		}

		public Task DeleteRemindersAsync(IReadOnlyDictionary<ActorId, IReadOnlyCollection<string>> reminderNames,
			CancellationToken cancellationToken = new CancellationToken())
		{
			return _innerStateProvider.DeleteRemindersAsync(reminderNames, cancellationToken);
		}

		public Func<CancellationToken, Task> OnRestoreCompletedAsync
		{
			set => _innerStateProvider.OnRestoreCompletedAsync = value;
		}
	}
}