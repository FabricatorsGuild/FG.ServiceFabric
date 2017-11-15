using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Actors.Runtime.Reminders
{
	public class ActorReminderCollection : IActorReminderCollection
	{
		private readonly ConcurrentDictionary<ActorId, IReadOnlyCollection<IActorReminderState>> reminderCollectionsByActorId;

		public ActorReminderCollection()
		{
			this.reminderCollectionsByActorId = new ConcurrentDictionary<ActorId, IReadOnlyCollection<IActorReminderState>>();
		}

		bool IReadOnlyDictionary<ActorId, IReadOnlyCollection<IActorReminderState>>.ContainsKey(ActorId key)
		{
			return this.reminderCollectionsByActorId.ContainsKey(key);
		}

		IEnumerable<ActorId> IReadOnlyDictionary<ActorId, IReadOnlyCollection<IActorReminderState>>.Keys
		{
			get { return this.reminderCollectionsByActorId.Keys; }
		}

		bool IReadOnlyDictionary<ActorId, IReadOnlyCollection<IActorReminderState>>.TryGetValue(ActorId key,
			out IReadOnlyCollection<IActorReminderState> value)
		{
			return this.reminderCollectionsByActorId.TryGetValue(key, out value);
		}

		IEnumerable<IReadOnlyCollection<IActorReminderState>>
			IReadOnlyDictionary<ActorId, IReadOnlyCollection<IActorReminderState>>.Values
		{
			get { return this.reminderCollectionsByActorId.Values; }
		}

		IReadOnlyCollection<IActorReminderState> IReadOnlyDictionary<ActorId, IReadOnlyCollection<IActorReminderState>>.
			this[ActorId key]
		{
			get { return this.reminderCollectionsByActorId[key]; }
		}

		int IReadOnlyCollection<KeyValuePair<ActorId, IReadOnlyCollection<IActorReminderState>>>.Count
		{
			get { return this.reminderCollectionsByActorId.Count; }
		}

		IEnumerator<KeyValuePair<ActorId, IReadOnlyCollection<IActorReminderState>>>
			IEnumerable<KeyValuePair<ActorId, IReadOnlyCollection<IActorReminderState>>>.GetEnumerator()
		{
			return this.reminderCollectionsByActorId.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.reminderCollectionsByActorId.GetEnumerator();
		}

		public void Add(ActorId actorId, IActorReminderState reminderState)
		{
			var collection = this.reminderCollectionsByActorId.GetOrAdd(
				actorId, k => new ConcurrentCollection<IActorReminderState>());

			((ConcurrentCollection<IActorReminderState>) collection).Add(reminderState);
		}

		#region Helper Class

		private class ConcurrentCollection<T> : IReadOnlyCollection<T>
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
	}
}