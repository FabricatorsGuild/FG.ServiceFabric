using System;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data;

namespace FG.ServiceFabric.Services.Runtime
{
	public abstract class WrappedStateProviderReplica : IStateProvider, IStateProviderReplica
	{
		public long GetLastCommittedSequenceNumber()
		{
			throw new NotImplementedException();
		}

		public Task UpdateEpochAsync(Epoch epoch, long previousEpochLastSequenceNumber, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		Task<bool> IStateProvider.OnDataLossAsync(CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public IOperationDataStream GetCopyContext()
		{
			throw new NotImplementedException();
		}

		public IOperationDataStream GetCopyState(long upToSequenceNumber, IOperationDataStream copyContext)
		{
			throw new NotImplementedException();
		}

		public void Initialize(StatefulServiceInitializationParameters initializationParameters)
		{
			throw new NotImplementedException();
		}

		public Task<IReplicator> OpenAsync(ReplicaOpenMode openMode, IStatefulServicePartition partition,
			CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public Task ChangeRoleAsync(ReplicaRole newRole, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public Task CloseAsync(CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public void Abort()
		{
			throw new NotImplementedException();
		}

		public Task BackupAsync(Func<BackupInfo, CancellationToken, Task<bool>> backupCallback)
		{
			throw new NotImplementedException();
		}

		public Task BackupAsync(BackupOption option, TimeSpan timeout, CancellationToken cancellationToken,
			Func<BackupInfo, CancellationToken, Task<bool>> backupCallback)
		{
			throw new NotImplementedException();
		}

		public Task RestoreAsync(string backupFolderPath)
		{
			throw new NotImplementedException();
		}

		public Task RestoreAsync(string backupFolderPath, RestorePolicy restorePolicy, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public Func<CancellationToken, Task<bool>> OnDataLossAsync { get; set; }
	}
}