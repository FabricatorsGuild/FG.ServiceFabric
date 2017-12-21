using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
                {
                    reminders.Add(actorReminderState.Name);
                }

                _reminderCollectionsByActorId.AddOrUpdate(actorId.Key, reminders, (id, collection) => reminders);
            }            
        }

        public IEnumerator<KeyValuePair<ActorId, IReadOnlyCollection<string>>> GetEnumerator()
        {
            return this._reminderCollectionsByActorId.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this._reminderCollectionsByActorId.GetEnumerator();
        }

        public int Count => this._reminderCollectionsByActorId.Count;
        public bool ContainsKey(ActorId key)
        {
            return this._reminderCollectionsByActorId.ContainsKey(key);
        }

        public bool TryGetValue(ActorId key, out IReadOnlyCollection<string> value)
        {
            return this._reminderCollectionsByActorId.TryGetValue(key, out value);
        }

        public IReadOnlyCollection<string> this[ActorId key] => this._reminderCollectionsByActorId[key];

        public IEnumerable<ActorId> Keys => this._reminderCollectionsByActorId.Keys;
        public IEnumerable<IReadOnlyCollection<string>> Values => this._reminderCollectionsByActorId.Values;
    }


    public class ActorReminderCollection : IActorReminderCollection
	{
		private readonly ConcurrentDictionary<ActorId, IReadOnlyCollection<IActorReminderState>> _reminderCollectionsByActorId;


		public ActorReminderCollection()
		{
			this._reminderCollectionsByActorId = new ConcurrentDictionary<ActorId, IReadOnlyCollection<IActorReminderState>>();
		}

		bool IReadOnlyDictionary<ActorId, IReadOnlyCollection<IActorReminderState>>.ContainsKey(ActorId key)
		{
			return this._reminderCollectionsByActorId.ContainsKey(key);
		}
        
	    public IEnumerable<ActorId> Keys { get; }
	    public IEnumerable<IReadOnlyCollection<string>> Values { get; }

	    IEnumerable<ActorId> IReadOnlyDictionary<ActorId, IReadOnlyCollection<IActorReminderState>>.Keys
		{
			get { return this._reminderCollectionsByActorId.Keys; }
		}

		bool IReadOnlyDictionary<ActorId, IReadOnlyCollection<IActorReminderState>>.TryGetValue(ActorId key,
			out IReadOnlyCollection<IActorReminderState> value)
		{
			return this._reminderCollectionsByActorId.TryGetValue(key, out value);
		}

		IEnumerable<IReadOnlyCollection<IActorReminderState>>
			IReadOnlyDictionary<ActorId, IReadOnlyCollection<IActorReminderState>>.Values
		{
			get { return this._reminderCollectionsByActorId.Values; }
		}

	    public bool ContainsKey(ActorId key)
	    {
	        return this._reminderCollectionsByActorId.ContainsKey(key);
	    }

	    IReadOnlyCollection<IActorReminderState> IReadOnlyDictionary<ActorId, IReadOnlyCollection<IActorReminderState>>.
			this[ActorId key]
		{
			get { return this._reminderCollectionsByActorId[key]; }
		}

		int IReadOnlyCollection<KeyValuePair<ActorId, IReadOnlyCollection<IActorReminderState>>>.Count
		{
			get { return this._reminderCollectionsByActorId.Count; }
		}
        
	    IEnumerator<KeyValuePair<ActorId, IReadOnlyCollection<IActorReminderState>>>
			IEnumerable<KeyValuePair<ActorId, IReadOnlyCollection<IActorReminderState>>>.GetEnumerator()
		{
			return this._reminderCollectionsByActorId.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this._reminderCollectionsByActorId.GetEnumerator();
		}

		public void Add(ActorId actorId, IActorReminderState reminderState)
		{
			var collection = this._reminderCollectionsByActorId.GetOrAdd(
				actorId, k => new ConcurrentCollection<IActorReminderState>());

			((ConcurrentCollection<IActorReminderState>) collection).Add(reminderState);
		}

		#region Helper Class

		internal class ConcurrentCollection<T> : IReadOnlyCollection<T>
		{
			private ConcurrentBag<T> concurrentBag;

			public ConcurrentCollection()
			{
				this.concurrentBag = new ConcurrentBag<T>();
			}

			int IReadOnlyCollection<T>.Count
			{
				get { return this.concurrentBag.Count; }
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return this.concurrentBag.GetEnumerator();
			}

			IEnumerator<T> IEnumerable<T>.GetEnumerator()
			{
				return this.concurrentBag.GetEnumerator();
			}

			public void Add(T item)
			{
				this.concurrentBag.Add(item);
			}
		}

		#endregion

	    public int Count { get; }
	}

}