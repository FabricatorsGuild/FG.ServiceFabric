using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Fabric;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Data;

namespace FG.ServiceFabric.Actors.Runtime
{
	public partial class StateSessionActorStateProvider : IStateProvider
	{
		#region IStateProviderReplica

		private ReplicaRole _currentRole;
		private StatefulServiceInitializationParameters _initParams;
		private IStateReplicator2 _replicator;
		private IStatefulServicePartition _servicePartition;
		private ActorTypeInformation _actorTypeInformation;
		private Func<CancellationToken, Task<bool>> _onDataLossFunc;

		#endregion

		#region IStateProviderReplica

		Func<CancellationToken, Task<bool>> IStateProviderReplica.OnDataLossAsync
		{
			set { this._onDataLossFunc = value; }
		}

		void IStateProviderReplica.Initialize(StatefulServiceInitializationParameters initializationParameters)
		{
			this._initParams = initializationParameters;
		}

		Task<IReplicator> IStateProviderReplica.OpenAsync(
			ReplicaOpenMode openMode,
			IStatefulServicePartition partition,
			CancellationToken cancellationToken)
		{
			var fabricReplicator = partition.CreateReplicator(this, this.GetReplicatorSettings());
			this._replicator = fabricReplicator.StateReplicator2;
			this._servicePartition = partition;

			return Task.FromResult<IReplicator>(fabricReplicator);
		}

		private ReplicatorSettings GetReplicatorSettings()
		{
			// Even though NullActorStateProvider don't replicate any state, we need
			// to keep the copy stream and the replication stream drained at all times.
			// This is required in order to unblock role changes. Hence we need to 
			// specify a valid replicator address in the settings.
			return ActorStateProviderHelper.GetActorReplicatorSettings(
				this._initParams.CodePackageActivationContext,
				this._actorTypeInformation.ImplementationType);
		}


		Task IStateProviderReplica.ChangeRoleAsync(ReplicaRole newRole, CancellationToken cancellationToken)
		{
			switch (newRole)
			{
				case ReplicaRole.IdleSecondary:
					this.StartSecondaryCopyAndReplicationPump();
					break;

				case ReplicaRole.ActiveSecondary:
					// Start replication pump if we are changing from primary
					if (this._currentRole == ReplicaRole.Primary)
					{
						this.StartSecondaryReplicationPump();
					}
					break;
			}

			this._currentRole = newRole;
			return Task.FromResult(true);
		}

		Task IStateProviderReplica.CloseAsync(CancellationToken cancellationToken)
		{
			return Task.FromResult(true);
		}

		void IStateProviderReplica.Abort()
		{
			// No-op
		}

		Task IStateProviderReplica.BackupAsync(Func<BackupInfo, CancellationToken, Task<bool>> backupCallback)
		{
			throw new NotImplementedException();
		}

		Task IStateProviderReplica.BackupAsync(
			BackupOption option,
			TimeSpan timeout,
			CancellationToken cancellationToken,
			Func<BackupInfo, CancellationToken, Task<bool>> backupCallback)
		{
			throw new NotImplementedException();
		}

		Task IStateProviderReplica.RestoreAsync(string backupFolderPath)
		{
			throw new NotImplementedException();
		}

		Task IStateProviderReplica.RestoreAsync(
			string backupFolderPath,
			RestorePolicy restorePolicy,
			CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		#endregion IStateProviderReplica

		#region IStateProvider

		IOperationDataStream IStateProvider.GetCopyContext()
		{
			return new NullOperationDataStream();
		}

		IOperationDataStream IStateProvider.GetCopyState(long upToSequenceNumber, IOperationDataStream copyContext)
		{
			return new NullOperationDataStream();
		}

		long IStateProvider.GetLastCommittedSequenceNumber()
		{
			return 0;
		}

		Task<bool> IStateProvider.OnDataLossAsync(CancellationToken cancellationToken)
		{
			return Task.FromResult(false);
		}

		Task IStateProvider.UpdateEpochAsync(Epoch epoch, long previousEpochLastSequenceNumber,
			CancellationToken cancellationToken)
		{
			return Task.FromResult<object>(null);
		}

		#endregion IStateProvider

		#region Secondary Pump Operation

		private void StartSecondaryCopyAndReplicationPump()
		{
			this.StartSecondaryPumpOperation(true);
		}

		private void StartSecondaryReplicationPump()
		{
			this.StartSecondaryPumpOperation(false);
		}

		private void StartSecondaryPumpOperation(bool isCopy)
		{
			Task.Run(
				async () =>
				{
					var operationStream = this.GetOperationStream(isCopy);

					try
					{
						var operation = await operationStream.GetOperationAsync(CancellationToken.None);

						if (operation == null)
						{
							// Since we are not replicating any data, we should always get null.
							//ActorTrace.Source.WriteInfoWithId(
							//	TraceType,
							//	this.traceId,
							//	"Reached end of operation stream (isCopy: {0}).",
							//	isCopy);

							if (isCopy)
							{
								// If we are doing copy operation, kick off replication pump now.
								this.StartSecondaryPumpOperation(false);
							}
						}
						else
						{
							// We don't expect any replication operations. It is an error if we get one.
							//ActorTrace.Source.WriteErrorWithId(
							//	TraceType,
							//	this.traceId,
							//	"An operation was unexpectedly received while pumping operation stream (isCopy: {0}).",
							//	isCopy);

							this._servicePartition.ReportFault(FaultType.Transient);
						}
					}
					catch (Exception)
					{
						// Failure to get operation stream usually mean the replica
						// is about to close, abort or change role to None.
						//ActorTrace.Source.WriteWarningWithId(
						//	TraceType,
						//	this.traceId,
						//	"Error while pumping operation stream (isCopy: {0}). Exception info: {1}",
						//	isCopy,
						//	ex.ToString());
					}
				});
		}

		#endregion Secondary Pump Operation

		#region Private Helper Classes

		private class NullOperationDataStream : IOperationDataStream
		{
			public Task<OperationData> GetNextAsync(CancellationToken cancellationToken)
			{
				return Task.FromResult<OperationData>(null);
			}
		}

		private IOperationStream GetOperationStream(bool isCopy)
		{
			return isCopy ? this._replicator.GetCopyStream() : this._replicator.GetReplicationStream();
		}

		#endregion #region Private Helper Classes
	}
}