using System;
using System.Collections.Generic;
using System.Fabric;
using System.Threading.Tasks;
using FG.ServiceFabric.Actors.Runtime;
using FG.ServiceFabric.Services.Runtime.State;
using Microsoft.ServiceFabric.Data;

namespace FG.ServiceFabric.Services.Runtime
{
	public class DocumentStorageStatefulServiceStateManager : WrappedStatefulServiceStateManager
	{
		private readonly StatefulServiceContext _serviceContext;
		private readonly Func<IDocumentStorageSession> _storageSessionFactory;

		public DocumentStorageStatefulServiceStateManager(
			StatefulServiceContext serviceContext,
			IStatefulServiceStateManager innerServiceStateManager,
			Func<IDocumentStorageSession> storageSessionFactory) : base(innerServiceStateManager)
		{
			_serviceContext = serviceContext;
			_storageSessionFactory = storageSessionFactory;
		}
	
		public override IStatefulServiceStateManagerSession CreateSession()
		{
			var innerSession = base.CreateSession();
			var storageSession = _storageSessionFactory();
			return new DocumentStorageStatefulServiceStateManagerSession(_serviceContext,  innerSession, storageSession);
		}
	}


	public class CombinationStorageManager : IStatefulServiceStateManager
	{
		public IStatefulServiceStateManagerSession CreateSession()
		{
			throw new NotImplementedException();
		}
	}

	public class CombinationStorageManagerSession : IStatefulServiceStateManagerSession
	{
		public void Dispose()
		{
			throw new NotImplementedException();
		}

		public Task<IStatefulServiceStateManagerSession> ForDictionary<T>(string schema)
		{
			throw new NotImplementedException();
		}

		public Task<IStatefulServiceStateManagerSession> ForQueue<T>(string schema)
		{
			throw new NotImplementedException();
		}

		public event EventHandler<SessionCommittedEventArgs> SessionCommitted;

		public Task SetAsync<T>(string schema, string storageKey, T value)
		{
			throw new NotImplementedException();
		}

		public Task<T> GetOrAddAsync<T>(string schema, string storageKey, Func<string, T> newValue)
		{
			throw new NotImplementedException();
		}

		public Task<ConditionalValue<T>> TryGetAsync<T>(string schema, string storageKey)
		{
			throw new NotImplementedException();
		}

		public Task RemoveAsync<T>(string schema, string storageKey)
		{
			throw new NotImplementedException();
		}

		public Task EnqueueAsync<T>(string schema, T value)
		{
			throw new NotImplementedException();
		}

		public Task<ConditionalValue<T>> DequeueAsync<T>(string schema)
		{
			throw new NotImplementedException();
		}

		public Task<ConditionalValue<T>> PeekAsync<T>(string schema)
		{
			throw new NotImplementedException();
		}

		public Task<IEnumerable<KeyValuePair<string, T>>> EnumerateDictionary<T>(string schema)
		{
			throw new NotImplementedException();
		}

		public Task<IEnumerable<T>> EnumerateQueue<T>(string schema)
		{
			throw new NotImplementedException();
		}

		public Task CommitAsync()
		{
			throw new NotImplementedException();
		}

		public Task AbortAsync()
		{
			throw new NotImplementedException();
		}
	}
}