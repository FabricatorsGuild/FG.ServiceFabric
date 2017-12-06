namespace FG.ServiceFabric.Services.Runtime.StateSession.Internal
{
	internal class StateSessionBaseObject<TStateSession> : IStateSessionObject
		where TStateSession : class, IStateSession
	{
		protected readonly IStateSessionManagerInternals _manager;
		protected readonly string _schema;
		private readonly bool _readOnly;
		protected TStateSession _session;

		protected StateSessionBaseObject(IStateSessionManagerInternals manager, string schema, bool readOnly)
		{
			_manager = manager;
			_schema = schema;
			_readOnly = readOnly;
		}

		public bool IsReadOnly => _readOnly;

		internal void AttachToSession(TStateSession session)
		{
			if (_session != null && _session.Equals(session))
			{
				throw new StateSessionException(
					$"Cannot attach StateSessionBaseDictionary to session {session.GetHashCode()}, it is already attached to session {_session.GetHashCode()}");
			}
			_session = session;
		}

		internal void DetachFromSession(TStateSession session)
		{
			if (_session == null || !_session.Equals(session))
			{
				throw new StateSessionException(
					$"Cannot detach StateSessionBaseDictionary from session {session.GetHashCode()}, it is not attached to this session {_session?.GetHashCode()}");
			}
			_session = null;
		}

		protected void CheckSession()
		{
			if (_session == null)
			{
				throw new StateSessionException(
					$"Cannot call methods on a StateSessionDictionary without a StateSession, call StateSessionManager.CreateSession() with this dictionary as an argument");
			}
		}

		public string Schema => _schema;
	}
}