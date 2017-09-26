using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FG.ServiceFabric.DocumentDb.CosmosDb;
using FG.ServiceFabric.Utils;
using Microsoft.Azure.Documents.Client;
using Microsoft.ServiceFabric.Actors.Query;

namespace FG.ServiceFabric.Services.Runtime.StateSession
{
	public class DocumentDbStateSessionManagerWithTransaction : TextStateSessionManagerWithTransaction
	{
		private readonly string _collection;
		private readonly string _databaseName;
		private readonly string _endpointUri;
		private readonly string _collectionPrimaryKey;
		private readonly ConnectionPolicySetting _connectionPolicySetting;

		private readonly ICosmosDbClientFactory _factory;

		public DocumentDbStateSessionManagerWithTransaction(
			string serviceName,
			Guid partitionId,
			string partitionKey,
			ISettingsProvider settingsProvider,
			ICosmosDbClientFactory factory = null,
			ConnectionPolicySetting connectionPolicySetting = ConnectionPolicySetting.GatewayHttps) :
			base(serviceName, partitionId, partitionKey)
		{
			_connectionPolicySetting = connectionPolicySetting;

			_factory = factory ?? new CosmosDbClientFactory();

			_collection = settingsProvider["Collection"];
			_databaseName = settingsProvider["DatabaseName"];
			_endpointUri = settingsProvider["EndpointUri"];
			_collectionPrimaryKey = settingsProvider["PrimaryKey"];
		}

		protected override TextStateSession CreateSessionInternal(StateSessionManagerBase<TextStateSession> manager)
		{
			return new DocumentDbStateSession(this);
		}

		public class DocumentDbStateSession : TextStateSessionManagerWithTransaction.TextStateSession
		{
			private readonly DocumentDbStateSessionManagerWithTransaction _manager;
			private readonly DocumentClient _documentClient;

			public DocumentDbStateSession(
				DocumentDbStateSessionManagerWithTransaction manager)
				: base(manager)
			{
				_manager = manager;
				_documentClient = _manager._factory.OpenAsync( // TODO: Do proper init.
					databaseName: _manager._databaseName,
					collection: new CosmosDbCollectionDefinition(_manager._collection, $"/partitionKey"),
					endpointUri: new Uri(_manager._endpointUri),
					primaryKey: _manager._collectionPrimaryKey,
					connectionPolicySetting: _manager._connectionPolicySetting).GetAwaiter().GetResult();
			}

			private Uri CreateDocumentUri(string schemaKey)
			{
				return UriFactory.CreateDocumentUri(DatabaseName, DatabaseCollection, schemaKey);
			}

			private string DatabaseName => _manager._databaseName;
			private string DatabaseCollection => _manager._collection;
			private Guid ServicePartitionId => _manager.PartitionId;
			private string ServicePartitionKey => _manager.PartitionKey;
			private string ServiceTypeName => _manager.ServiceName;

			protected override string Read(string id, bool checkExistsOnly = false)
			{
				throw new NotImplementedException();
			}

			protected override void Delete(string id)
			{
				throw new NotImplementedException();
			}

			protected override void Write(string id, string content)
			{
				throw new NotImplementedException();
			}

			protected override FindByKeyPrefixResult Find(string idPrefix, string key, int maxNumResults = 100000, ContinuationToken continuationToken = null,
				CancellationToken cancellationToken = new CancellationToken())
			{
				throw new NotImplementedException();
			}

			protected override Task CommitinternalAsync(IEnumerable<StateChange> stateChanges)
			{
				return base.CommitinternalAsync(stateChanges);
			}
		}
	}
}