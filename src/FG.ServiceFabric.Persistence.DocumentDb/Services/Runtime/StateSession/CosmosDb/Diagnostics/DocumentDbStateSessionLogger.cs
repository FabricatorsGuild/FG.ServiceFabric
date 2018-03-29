/*******************************************************************************************
*  This class is autogenerated from the class DocumentDbStateSessionLogger
*  Do not directly update this class as changes will be lost on rebuild.
*******************************************************************************************/
using System;
using System.Collections.Generic;
using FG.ServiceFabric.Services.Runtime.StateSession.CosmosDb;



namespace FG.ServiceFabric.Services.Runtime.StateSession.CosmosDb.Diagnostics
{
	internal sealed class DocumentDbStateSessionLogger : IDocumentDbStateSessionLogger
	{

        private sealed class ScopeWrapper : IDisposable
        {
            private readonly IEnumerable<IDisposable> _disposables;

            public ScopeWrapper(IEnumerable<IDisposable> disposables)
            {
                _disposables = disposables;
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            private void Dispose(bool disposing)
            {
                if (disposing)
                {
                    foreach (var disposable in _disposables)
                    {
                        disposable.Dispose();
                    }
                }
            }
        }

	    private sealed class ScopeWrapperWithAction : IDisposable
        {
            private readonly Action _onStop;

            internal static IDisposable Wrap(Func<IDisposable> wrap)
            {
                return wrap();
            }

            public ScopeWrapperWithAction(Action onStop)
            {
                _onStop = onStop;
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            private void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _onStop?.Invoke();
                }
            }
        }

		private readonly string _managerInstance;
		private readonly string _sessionInstance;

		public DocumentDbStateSessionLogger(
			string managerInstance,
			string sessionInstance)
		{
			_managerInstance = managerInstance;
			_sessionInstance = sessionInstance;
		}

		public void StartingSession(
			string stateObjects)
		{
			FGServiceFabricPersistenceEventSource.Current.StartingSession(
				_managerInstance, 
				_sessionInstance, 
				stateObjects
			);
    
		}


		public void DocumentClientException(
			string stateSessionOperation,
			int documentClientStatusCode,
			Microsoft.Azure.Documents.Error error,
			string message)
		{
			FGServiceFabricPersistenceEventSource.Current.DocumentClientException(
				_managerInstance, 
				_sessionInstance, 
				stateSessionOperation, 
				documentClientStatusCode, 
				error, 
				message
			);
    
		}


	}
}
