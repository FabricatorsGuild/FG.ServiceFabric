using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FG.ServiceFabric.Services.Runtime.StateSession;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Query;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Actors.Runtime
{
	public class DocumentStorageActorStateProvider : WrappedActorStateProvider
    {
	    internal static class DebugHelper
		{
			public static DocumentStorageActorStateProvider AddActorId(DocumentStorageActorStateProvider provider, ActorId actorId)
			{
				var stateKey = provider.GetActorIdStateKey(actorId);
				var state = new ExternalState() { Value = actorId, Key = stateKey };
				provider._documentStorageSession.UpsertAsync(stateKey, state).GetAwaiter().GetResult();				
				return provider;
			}
			public static DocumentStorageActorStateProvider AddActorState(DocumentStorageActorStateProvider provider, ActorId actorId, string stateName, object value)
			{
				var stateValue = value;
				var state = new ExternalState() { Key = provider.GetActorStateKey(actorId, stateName), Value = stateValue};

				provider._documentStorageSession.UpsertAsync(state.Key, state).GetAwaiter().GetResult();

				return provider;
			}
			public static DocumentStorageActorStateProvider AddActorState(DocumentStorageActorStateProvider provider, ActorId actorId, string stateName, object value, string type)
			{
				var stateKey = provider.GetActorStateKey(actorId, stateName);
				var state = new ExternalState() { Key = provider.GetActorStateKey(actorId, stateName), Value = value};

				provider._documentStorageSession.UpsertAsync(stateKey, state).GetAwaiter().GetResult();

				return provider;
			}
		}

	    private readonly IStateSession _stateSession;
		private readonly IDocumentStorageSession _documentStorageSession;

		public DocumentStorageActorStateProvider(
			IActorStateProvider stateProvider = null, 
			ActorTypeInformation actorTypeInfo = null, 
			IDocumentStorageSession documentStorageSession = null) : base(stateProvider, actorTypeInfo)
		{
			_documentStorageSession = documentStorageSession ?? new InMemoryDocumentStorageSession();
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

	    public override void Initialize(ActorTypeInformation actorTypeInformation)
		{
			base.Initialize(actorTypeInformation);
		}

		public override async Task ActorActivatedAsync(ActorId actorId, CancellationToken cancellationToken = new CancellationToken())
	    {
		    await base.ActorActivatedAsync(actorId, cancellationToken);

			// Save to database?
		    var stateKey = GetActorIdStateKey(actorId);
			var externalContainsState = await _documentStorageSession.ContainsAsync(stateKey);
		    if (!externalContainsState)
		    {
			    var externalState = new ExternalState() {Key = stateKey, Value = new ExternalActorIdState(actorId)};
				await _documentStorageSession.UpsertAsync(stateKey, externalState);				
		    }
		}

		public override async Task<bool> ContainsStateAsync(ActorId actorId, string stateName, CancellationToken cancellationToken = new CancellationToken())
	    {
			var innerContainsState = await base.ContainsStateAsync(actorId, stateName, cancellationToken);
		    if (!innerContainsState)
		    {
			    try
			    {
				    // Load from database?
				    var stateKey = GetActorStateKey(actorId, stateName);
				    var externalContainsState = await _documentStorageSession.ContainsAsync(stateKey);

				    if (externalContainsState)
				    {
					    var externalState = await _documentStorageSession.ReadAsync(stateKey);
					    var externalStateObject = externalState.Value;
					    var externalStateType = externalStateObject.GetType();

						var internalStateChange = new ActorStateChange(stateName, externalStateType, externalStateObject, StateChangeKind.Add);

						// TODO: Check, Can this fail???
					    await base.SaveStateAsync(actorId, new ActorStateChange[] {internalStateChange}, cancellationToken);

					    return true;
				    }

			    }
			    catch (Exception ex)
			    {
				    // Log this
					// TODO: Add this to a list of unsynced changes???
			    }
			}
		    return innerContainsState;
	    }

	    public override async Task<IEnumerable<string>> EnumerateStateNamesAsync(ActorId actorId, CancellationToken cancellationToken = new CancellationToken())
	    {
		    var internalStates = await base.EnumerateStateNamesAsync(actorId, cancellationToken);

			var stateNames = new List<string>(internalStates);

		    if (!stateNames.Any())
		    {
			    try
			    {
				    // Load from database?
				    var stateKeyPrefix = GetActorStateKeyPrefix(actorId);
				    var externalContainsStates = await _documentStorageSession.FindByKeyAsync(stateKeyPrefix);

				    var patchInternalState = new List<ActorStateChange>();
				    foreach (var externalContainedStateName in externalContainsStates)
				    {
					    stateNames.Add(externalContainedStateName);
					    var externalContainedState = await _documentStorageSession.ReadAsync(externalContainedStateName);

						var stateName = externalContainedState.Key;
					    var externalStateObject = externalContainedState.Value;
					    var externalStateType = externalStateObject.GetType();

						var internalStateChange = new ActorStateChange(stateName, externalStateType, externalStateObject, StateChangeKind.Add);
						patchInternalState.Add(internalStateChange);
					}

					// TODO: Check, Can this fail???
					await base.SaveStateAsync(actorId, patchInternalState, cancellationToken);
				}
				catch (Exception ex)
			    {
				    // Log this
				    // TODO: Add this to a list of unsynced changes???
			    }
		    }

		    return stateNames;
	    }

	    public override async Task<PagedResult<ActorId>> GetActorsAsync(int numItemsToReturn, ContinuationToken continuationToken, CancellationToken cancellationToken)
	    {
			var actors = new List<ActorId>();
			var lastActorInResult = (string)null;

			try
			{
				// Load from database?
				var stateKeyPrefix = GetActorIdStateKeyPrefix();
				var lastActorKey = continuationToken?.Marker as string;
				var externalContainsStates = await _documentStorageSession.FindByKeyAsync(stateKeyPrefix, numItemsToReturn, lastActorKey);

				foreach (var externalContainedStateName in externalContainsStates)
				{
					var externalState = await _documentStorageSession.ReadAsync(externalContainedStateName);
					var externalStateObject = (externalState.Value as ExternalActorIdState)?.ToActorId();

					await base.ActorActivatedAsync(externalStateObject, cancellationToken);
					actors.Add(externalStateObject);
					lastActorInResult = externalStateObject?.ToString();
				}
				
			}
			catch (Exception ex)
			{
				// Log this
				// TODO: Add this to a list of unsynced changes???
			}

			var result = new PagedResult<ActorId> { Items = actors, ContinuationToken = lastActorInResult != null ? new ContinuationToken(lastActorInResult) : null};
			return result;
		}

		public override async Task<T> LoadStateAsync<T>(ActorId actorId, string stateName, CancellationToken cancellationToken = new CancellationToken())
		{
			var containsInteralState = await base.ContainsStateAsync(actorId, stateName, cancellationToken);

			if (containsInteralState)
			{
				var internalState = await base.LoadStateAsync<T>(actorId, stateName, cancellationToken);
				return internalState;
			}
		    else
		    {
				try
				{
					// Load from database?
					var stateKey = GetActorStateKey(actorId, stateName);
					var containsExternalContainsState = await _documentStorageSession.ContainsAsync(stateKey);

					if (containsExternalContainsState)
					{
						var externalState = await _documentStorageSession.ReadAsync(stateKey);
						var externalStateObject = externalState.Value;
						var externalStateType = externalStateObject.GetType();

						var internalStateChange = new ActorStateChange(stateName, externalStateType, externalStateObject, StateChangeKind.Add);

						// TODO: Check, Can this fail???
						await base.SaveStateAsync(actorId, new ActorStateChange[] {internalStateChange}, cancellationToken);

						return (T)externalStateObject;
					}
				}
				catch (Exception ex)
				{
					// Log this
					// TODO: Add this to a list of unsynced changes???
					throw new KeyNotFoundException($"Actor State with name {stateName} was not found", ex);
				}
				throw new KeyNotFoundException($"Actor State with name {stateName} was not found");
			}
		}

	    public override async Task SaveStateAsync(ActorId actorId, IReadOnlyCollection<ActorStateChange> stateChanges, CancellationToken cancellationToken = new CancellationToken())
	    {
		    await base.SaveStateAsync(actorId, stateChanges, cancellationToken);

			// Save to database
			try
			{
				foreach (var stateChange in stateChanges)
				{
					var stateName = stateChange.StateName;
					var stateKey = GetActorStateKey(actorId, stateName);

					switch (stateChange.ChangeKind)
					{
						case (StateChangeKind.Add):
						case (StateChangeKind.Update):

							var externalState = new ExternalState()
							{
								Key = stateKey,
								Value = stateChange.Value,
							};

							await _documentStorageSession.UpsertAsync(stateKey, externalState);

							break;

						case (StateChangeKind.Remove):

							await _documentStorageSession.DeleteAsync(stateKey);

							break;
					}

					
				}
			}
			catch (Exception ex)
			{
				// Log this
				// TODO: Add this to a list of unsynced changes???
			}
		}

	    public override async Task RemoveActorAsync(ActorId actorId, CancellationToken cancellationToken = new CancellationToken())
	    {
		    await base.RemoveActorAsync(actorId, cancellationToken);

			// Save to database
			try
			{
				var stateKeyPrefix = GetActorStateKeyPrefix(actorId);
				var stateKeys = await _documentStorageSession.FindByKeyAsync(stateKeyPrefix);
				foreach (var stateKey in stateKeys)
				{
					await _documentStorageSession.DeleteAsync(stateKey);
				}

				var actorIdStateKey = GetActorStateKeyPrefix(actorId);
				await _documentStorageSession.DeleteAsync(actorIdStateKey);
			}
			catch (Exception ex)
			{
				// Log this
				// TODO: Add this to a list of unsynced changes???
			}

		}
    }
}