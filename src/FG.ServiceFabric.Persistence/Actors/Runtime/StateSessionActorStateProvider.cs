using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FG.ServiceFabric.Actors.Runtime.Reminders;
using FG.ServiceFabric.Actors.Runtime.StateSession.Metadata;
using FG.ServiceFabric.Services.Runtime.StateSession;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Query;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Actors.Runtime
{
	public partial class StateSessionActorStateProvider : IActorStateProvider
	{
		//private IActorStateProvider _actorStateProvider;
		private readonly IStateSessionManager _stateSessionManager;

		private IStateSession _stateSession;

		public StateSessionActorStateProvider(ServiceContext context, IStateSessionManager stateSessionManager,
			ActorTypeInformation actorTypeInfo)
		{
			_actorTypeInformation = actorTypeInfo;
			_stateSessionManager = stateSessionManager;
		}

		public void Initialize(ActorTypeInformation actorTypeInformation)
		{
			this._actorTypeInformation = actorTypeInformation;
		}

		public async Task ActorActivatedAsync(ActorId actorId,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;

			var key = StateSessionHelper.GetActorIdSchemaKey(actorId);

			var existingActor = await GetSession()
				.TryGetValueAsync<string>(StateSessionHelper.ActorIdStateSchemaName, key, cancellationToken);
			if (!existingActor.HasValue)
			{
				var actorIdState = StateSessionHelper.GetActorIdSchemaKey(actorId);
				// e.g.: servicename_partition1_ACTORID_G:A4F3A8FC-801E-4940-8993-98CB6D7BCEF9
				var metadata = new ActorStateValueMetadata(StateWrapperType.ActorId, actorId);
				await GetSession().SetValueAsync(StateSessionHelper.ActorIdStateSchemaName, key, actorIdState, metadata,
					cancellationToken);
			}
		}

		public Func<CancellationToken, Task> OnRestoreCompletedAsync { get; set; }

		private IStateSession GetSession()
		{
			return _stateSession ?? (_stateSession = _stateSessionManager.CreateSession());
		}


		public void Abort()
		{
		}

		#region Actor State

		public async Task<T> LoadStateAsync<T>(ActorId actorId, string actorStateName,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;

			var schemaName = StateSessionHelper.GetActorStateSchemaName(actorStateName);
			var key = StateSessionHelper.GetActorIdSchemaKey(actorId);

			// e.g.: servicename_partition1_ACTORSTATE-myState_G:A4F3A8FC-801E-4940-8993-98CB6D7BCEF9
			var state = await GetSession().GetValueAsync<T>(schemaName, key, cancellationToken);
			return state;
		}

		public async Task SaveStateAsync(ActorId actorId, IReadOnlyCollection<ActorStateChange> stateChanges,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;

			foreach (var actorStateChange in stateChanges)
			{
				var schemaName = StateSessionHelper.GetActorStateSchemaName(actorStateChange.StateName);
				var key = StateSessionHelper.GetActorIdSchemaKey(actorId);

				switch (actorStateChange.ChangeKind)
				{
					case (StateChangeKind.Add):
					case (StateChangeKind.Update):

						// e.g.: servicename_partition1_ACTORSTATE-myState_G:A4F3A8FC-801E-4940-8993-98CB6D7BCEF9
						var metadata = new ActorStateValueMetadata(StateWrapperType.ActorState, actorId);
						await GetSession().SetValueAsync(schemaName, key, actorStateChange.Type, actorStateChange.Value, metadata,
							cancellationToken);

						break;
					case (StateChangeKind.Remove):

						// e.g.: servicename_partition1_ACTORSTATE-myState_G:A4F3A8FC-801E-4940-8993-98CB6D7BCEF9
						await GetSession().RemoveAsync(schemaName, key, cancellationToken);

						break;
					case (StateChangeKind.None):
						break;
				}
			}
			await GetSession().CommitAsync();
		}

		public Task<bool> ContainsStateAsync(ActorId actorId, string actorStateName,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;

			var schemaName = StateSessionHelper.GetActorStateSchemaName(actorStateName);
			var key = StateSessionHelper.GetActorIdSchemaKey(actorId);

			// e.g.: servicename_partition1_ACTORSTATE-myState_G:A4F3A8FC-801E-4940-8993-98CB6D7BCEF9
			return GetSession().Contains(schemaName, key, cancellationToken);
		}

		public async Task RemoveActorAsync(ActorId actorId, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;

			// Save to database
			try
			{
				var key = StateSessionHelper.GetActorIdSchemaKey(actorId);

				var schemaNames = await GetSession().EnumerateSchemaNamesAsync(key, cancellationToken);
				foreach (var schemaName in schemaNames)
				{
					// e.g.: servicename_partition1_ACTORSTATE-myState_G:A4F3A8FC-801E-4940-8993-98CB6D7BCEF9
					await GetSession().RemoveAsync(schemaName, key, cancellationToken);
				}
				await GetSession().CommitAsync();
			}
			catch (Exception ex)
			{
				throw new SessionStateActorStateProviderException($"Failed to RemoveActorAsync", ex);
			}
		}

		public async Task<IEnumerable<string>> EnumerateStateNamesAsync(ActorId actorId,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;

			// Save to database
			try
			{
				var key = StateSessionHelper.GetActorIdSchemaKey(actorId);
				var baseSchemaNames = await GetSession().EnumerateSchemaNamesAsync(key, cancellationToken);

				// e.g.: servicename_partition1_ACTORSTATE-xyz_G:A4F3A8FC-801E-4940-8993-98CB6D7BCEF9
				return baseSchemaNames
					.Where(schema => schema.StartsWith(StateSessionHelper.ActorStateSchemaName))
					.Select(schema => schema.Substring(StateSessionHelper.ActorStateSchemaName.Length + 1)).ToArray();
			}
			catch (Exception ex)
			{
				throw new SessionStateActorStateProviderException($"Failed to EnumerateStateNamesAsync", ex);
			}
		}

		public async Task<PagedResult<ActorId>> GetActorsAsync(int numItemsToReturn, ContinuationToken continuationToken,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;

			// Save to database
			try
			{
				var schemaName = StateSessionHelper.ActorIdStateSchemaName;

				var result = await GetSession()
					.FindByKeyPrefixAsync<string>(schemaName, null, numItemsToReturn, continuationToken, cancellationToken);
				// e.g.: servicename_partition1_ACTORID_G:A4F3A8FC-801E-4940-8993-98CB6D7BCEF9
				var actorIds = result.Items.Select(StateSessionHelper.TryGetActorIdFromSchemaKey).ToArray();
				return new PagedResult<ActorId>() {Items = actorIds, ContinuationToken = result.ContinuationToken};
			}
			catch (Exception ex)
			{
				throw new SessionStateActorStateProviderException($"Failed to EnumerateStateNamesAsync", ex);
			}
		}

		#endregion

		#region Actor Reminders

		public async Task SaveReminderAsync(ActorId actorId, IActorReminder reminder,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;

			try
			{
				var schemaName = StateSessionHelper.ActorReminderSchemaName;
				var stateName = StateSessionHelper.GetActorReminderSchemaKey(actorId, reminder.Name);

				var actorReminderData = new ActorReminderData(actorId, reminder.Name, reminder.DueTime, reminder.Period,
					reminder.State, DateTime.UtcNow);

				// e.g.: servicename_partition1_ACTORREMINDER_G:A4F3A8FC-801E-4940-8993-98CB6D7BCEF9-wakeupcall
				var metadata = new ActorStateValueMetadata(StateWrapperType.ActorReminder, actorId);
				await GetSession().SetValueAsync(schemaName, stateName, actorReminderData, metadata, cancellationToken);
				await GetSession().CommitAsync();
			}
			catch (Exception ex)
			{
				throw new SessionStateActorStateProviderException($"Failed to EnumerateStateNamesAsync", ex);
			}
		}

		public async Task DeleteReminderAsync(ActorId actorId, string reminderName,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;

			try
			{
				var schemaName = StateSessionHelper.ActorReminderSchemaName;
				var stateName = StateSessionHelper.GetActorReminderSchemaKey(actorId, reminderName);

				// e.g.: servicename_partition1_ACTORREMINDER_G:A4F3A8FC-801E-4940-8993-98CB6D7BCEF9-wakeupcall
				await GetSession().RemoveAsync(schemaName, stateName, cancellationToken);
				await GetSession().CommitAsync();
			}
			catch (Exception ex)
			{
				throw new SessionStateActorStateProviderException($"Failed to EnumerateStateNamesAsync", ex);
			}
		}

		public async Task DeleteRemindersAsync(IReadOnlyDictionary<ActorId, IReadOnlyCollection<string>> reminderNames,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;

			try
			{
				foreach (var reminderNamesByActor in reminderNames)
				{
					var actorId = reminderNamesByActor.Key;
					foreach (var actorReminderName in reminderNamesByActor.Value)
					{
						var schemaName = StateSessionHelper.ActorReminderSchemaName;
						var stateName = StateSessionHelper.GetActorReminderSchemaKey(actorId, actorReminderName);

						// e.g.: servicename_partition1_ACTORREMINDER_G:A4F3A8FC-801E-4940-8993-98CB6D7BCEF9-wakeupcall
						await GetSession().RemoveAsync(schemaName, stateName, cancellationToken);
					}
				}
				await GetSession().CommitAsync();
			}
			catch (Exception ex)
			{
				throw new SessionStateActorStateProviderException($"Failed to EnumerateStateNamesAsync", ex);
			}
		}

		public async Task<IActorReminderCollection> LoadRemindersAsync(
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;

			try
			{
				var reminderCollection = new ActorReminderCollection();

				// e.g.: servicename_partition1_ACTORREMINDER_G:A4F3A8FC-801E-4940-8993-98CB6D7BCEF9-wakeupcall
				var reminderKeys = await GetSession()
					.FindByKeyPrefixAsync<ActorReminderData>(StateSessionHelper.ActorReminderSchemaName, null,
						cancellationToken: cancellationToken);

				foreach (var reminderKey in reminderKeys.Items)
				{
					// e.g.: servicename_partition1_ACTORREMINDER_G:A4F3A8FC-801E-4940-8993-98CB6D7BCEF9-wakeupcall
					var reminder = await GetSession().TryGetValueAsync<ActorReminderData>(StateSessionHelper.ActorReminderSchemaName,
						reminderKey, cancellationToken);
					if (reminder.HasValue)
					{
						var reminderData = reminder.Value;
						// e.g.: servicename_partition1_ACTORREMINDERCOMPLETED_G:A4F3A8FC-801E-4940-8993-98CB6D7BCEF9-wakeupcall
						var reminderCompleted = await GetSession()
							.TryGetValueAsync<ActorReminderCompletedData>(StateSessionHelper.ActorReminderCompletedSchemaName, reminderKey,
								cancellationToken);
						var reminderCompletedData = reminderCompleted.HasValue ? reminderCompleted.Value : null;
						reminderCollection.Add(
							reminderData.ActorId,
							new ActorReminderState(reminderData, DateTime.UtcNow, reminderCompletedData));
					}
				}

				return reminderCollection;
			}
			catch (Exception ex)
			{
				throw new SessionStateActorStateProviderException($"Failed to LoadRemindersAsync", ex);
			}
		}

		public async Task ReminderCallbackCompletedAsync(ActorId actorId, IActorReminder reminder,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;

			try
			{
				var schemaName = StateSessionHelper.ActorReminderCompletedSchemaName;
				var key = StateSessionHelper.GetActorReminderSchemaKey(actorId, reminder.Name);

				var reminderCompletedState = new ActorReminderCompletedData(actorId, reminder.Name, DateTime.UtcNow);
				// e.g.: servicename_partition1_ACTORREMINDER_G:A4F3A8FC-801E-4940-8993-98CB6D7BCEF9-wakeupcall
				var metadata = new ActorStateValueMetadata(StateWrapperType.ActorReminderCompleted, actorId);
				await GetSession().SetValueAsync(schemaName, key, reminderCompletedState, metadata, cancellationToken);
				await GetSession().CommitAsync();
			}
			catch (Exception ex)
			{
				throw new SessionStateActorStateProviderException($"Failed on ReminderCallbackCompletedAsync", ex);
			}
		}

		#endregion
	}
}