using System.Threading;
using System.Threading.Tasks;
using FG.ServiceFabric.Services.Runtime.StateSession.Metadata;
using Microsoft.ServiceFabric.Data;

namespace FG.ServiceFabric.Services.Runtime.StateSession.Internal
{
    internal class StateSessionBaseReadOnlyQueue<TStateSession, TValueType> : StateSessionBaseObject<TStateSession>,
        IStateSessionReadOnlyQueue<TValueType>, IAsyncEnumerable<TValueType>
        where TStateSession : class, IStateSession
    {
        public StateSessionBaseReadOnlyQueue(IStateSessionManagerInternals manager, string schema, bool readOnly)
            : base(manager, schema, readOnly)
        {
        }

        IAsyncEnumerator<TValueType> IAsyncEnumerable<TValueType>.GetAsyncEnumerator()
        {
            return new StateSessionBaseQueueEnumerator(_manager, _session, _schema);
        }

        public Task<ConditionalValue<TValueType>> PeekAsync(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckSession();
            return _session.PeekAsync<TValueType>(_schema, cancellationToken);
        }

        public Task<IAsyncEnumerable<TValueType>> CreateEnumerableAsync()
        {
            CheckSession();
            return Task.FromResult((IAsyncEnumerable<TValueType>) this);
        }

        public Task<long> GetCountAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckSession();
            return _session.GetEnqueuedCountAsync<TValueType>(_schema, cancellationToken);
        }

        private class StateSessionBaseQueueEnumerator : IAsyncEnumerator<TValueType>
        {
            private readonly ISchemaKey _queueInfoSchemaKey;
            private readonly string _schema;
            private long _currentIndex;
            private ConditionalValue<QueueInfo> _queueInfo;
            private TStateSession _session;

            public StateSessionBaseQueueEnumerator(IStateSessionManagerInternals manager, TStateSession session,
                string schema)
            {
                _session = session;
                _schema = schema;

                _queueInfoSchemaKey = new QueueInfoStateKey(_schema);
                _queueInfo = new ConditionalValue<QueueInfo>(false, null);

                Reset();
            }

            private string CurrentKey { get; set; }

            public void Dispose()
            {
                Current = default(TValueType);
                _session = null;
            }

            public async Task<bool> MoveNextAsync(CancellationToken cancellationToken)
            {
                if (!_queueInfo.HasValue)
                {
                    _queueInfo =
                        await _session.TryGetValueAsync<QueueInfo>(_schema, _queueInfoSchemaKey.Key, cancellationToken);
                    if (!_queueInfo.HasValue)
                    {
                        Current = default(TValueType);
                        return false;
                    }
                }

                if (_currentIndex < 0L)
                    _currentIndex = _queueInfo.Value.TailKey;

                if (_currentIndex > _queueInfo.Value.HeadKey)
                {
                    Current = default(TValueType);
                    return false;
                }

                var currentKey = new QueueItemStateKey(_schema, _currentIndex).Key;
                Current = await _session.GetValueAsync<TValueType>(_schema, currentKey, cancellationToken);

                _currentIndex++;
                return true;
            }

            public void Reset()
            {
                _currentIndex = -1L;
                Current = default(TValueType);
                CurrentKey = null;
            }

            public TValueType Current { get; private set; }
        }
    }

    internal class StateSessionBaseQueue<TStateSession, TValueType> :
        StateSessionBaseReadOnlyQueue<TStateSession, TValueType>,
        IStateSessionQueue<TValueType>, IAsyncEnumerable<TValueType>
        where TStateSession : class, IStateSession
    {
        public StateSessionBaseQueue(IStateSessionManagerInternals manager, string schema, bool readOnly)
            : base(manager, schema, readOnly)
        {
        }

        public Task EnqueueAsync(TValueType value, CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckSession();
            return _session.EnqueueAsync(_schema, value, null, cancellationToken);
        }

        public Task EnqueueAsync(TValueType value, IValueMetadata metadata,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckSession();
            return _session.EnqueueAsync(_schema, value, metadata, cancellationToken);
        }

        public Task<ConditionalValue<TValueType>> DequeueAsync(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckSession();
            return _session.DequeueAsync<TValueType>(_schema, cancellationToken);
        }
    }
}