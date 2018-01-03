using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Actors.Runtime.Reminders
{
    public class ActorReminderNamesCollection : IReadOnlyDictionary<ActorId, IReadOnlyCollection<string>>
    {
        private readonly ConcurrentDictionary<ActorId, IReadOnlyCollection<string>> _reminderCollectionsByActorId;

        public ActorReminderNamesCollection(IActorReminderCollection actorReminderCollection)
        {
            _reminderCollectionsByActorId = new ConcurrentDictionary<ActorId, IReadOnlyCollection<string>>();
            foreach (var actorId in actorReminderCollection)
            {
                var reminders = new ActorReminderCollection.ConcurrentCollection<string>();

                foreach (var actorReminderState in actorId.Value)
                    reminders.Add(actorReminderState.Name);

                _reminderCollectionsByActorId.AddOrUpdate(actorId.Key, reminders, (id, collection) => reminders);
            }
        }

        public IEnumerator<KeyValuePair<ActorId, IReadOnlyCollection<string>>> GetEnumerator()
        {
            return _reminderCollectionsByActorId.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _reminderCollectionsByActorId.GetEnumerator();
        }

        public int Count => _reminderCollectionsByActorId.Count;

        public bool ContainsKey(ActorId key)
        {
            return _reminderCollectionsByActorId.ContainsKey(key);
        }

        public bool TryGetValue(ActorId key, out IReadOnlyCollection<string> value)
        {
            return _reminderCollectionsByActorId.TryGetValue(key, out value);
        }

        public IReadOnlyCollection<string> this[ActorId key] => _reminderCollectionsByActorId[key];

        public IEnumerable<ActorId> Keys => _reminderCollectionsByActorId.Keys;
        public IEnumerable<IReadOnlyCollection<string>> Values => _reminderCollectionsByActorId.Values;
    }


    public class ActorReminderCollection : IActorReminderCollection
    {
        private readonly ConcurrentDictionary<ActorId, IReadOnlyCollection<IActorReminderState>>
            _reminderCollectionsByActorId;


        public ActorReminderCollection()
        {
            _reminderCollectionsByActorId =
                new ConcurrentDictionary<ActorId, IReadOnlyCollection<IActorReminderState>>();
        }

        public IEnumerable<ActorId> Keys { get; }
        public IEnumerable<IReadOnlyCollection<string>> Values { get; }

        public int Count { get; }

        bool IReadOnlyDictionary<ActorId, IReadOnlyCollection<IActorReminderState>>.ContainsKey(ActorId key)
        {
            return _reminderCollectionsByActorId.ContainsKey(key);
        }

        IEnumerable<ActorId> IReadOnlyDictionary<ActorId, IReadOnlyCollection<IActorReminderState>>.Keys =>
            _reminderCollectionsByActorId.Keys;

        bool IReadOnlyDictionary<ActorId, IReadOnlyCollection<IActorReminderState>>.TryGetValue(ActorId key,
            out IReadOnlyCollection<IActorReminderState> value)
        {
            return _reminderCollectionsByActorId.TryGetValue(key, out value);
        }

        IEnumerable<IReadOnlyCollection<IActorReminderState>>
            IReadOnlyDictionary<ActorId, IReadOnlyCollection<IActorReminderState>>.Values =>
            _reminderCollectionsByActorId.Values;

        IReadOnlyCollection<IActorReminderState> IReadOnlyDictionary<ActorId, IReadOnlyCollection<IActorReminderState>>.
            this[ActorId key] => _reminderCollectionsByActorId[key];

        int IReadOnlyCollection<KeyValuePair<ActorId, IReadOnlyCollection<IActorReminderState>>>.Count =>
            _reminderCollectionsByActorId.Count;

        IEnumerator<KeyValuePair<ActorId, IReadOnlyCollection<IActorReminderState>>>
            IEnumerable<KeyValuePair<ActorId, IReadOnlyCollection<IActorReminderState>>>.GetEnumerator()
        {
            return _reminderCollectionsByActorId.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _reminderCollectionsByActorId.GetEnumerator();
        }

        public bool ContainsKey(ActorId key)
        {
            return _reminderCollectionsByActorId.ContainsKey(key);
        }

        public void Add(ActorId actorId, IActorReminderState reminderState)
        {
            var collection = _reminderCollectionsByActorId.GetOrAdd(
                actorId, k => new ConcurrentCollection<IActorReminderState>());

            ((ConcurrentCollection<IActorReminderState>) collection).Add(reminderState);
        }

        #region Helper Class

        internal class ConcurrentCollection<T> : IReadOnlyCollection<T>
        {
            private readonly ConcurrentBag<T> concurrentBag;

            public ConcurrentCollection()
            {
                concurrentBag = new ConcurrentBag<T>();
            }

            int IReadOnlyCollection<T>.Count => concurrentBag.Count;

            IEnumerator IEnumerable.GetEnumerator()
            {
                return concurrentBag.GetEnumerator();
            }

            IEnumerator<T> IEnumerable<T>.GetEnumerator()
            {
                return concurrentBag.GetEnumerator();
            }

            public void Add(T item)
            {
                concurrentBag.Add(item);
            }
        }

        #endregion
    }
}