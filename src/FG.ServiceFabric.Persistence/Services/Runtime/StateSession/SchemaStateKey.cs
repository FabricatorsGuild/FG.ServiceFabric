using System;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.ServiceFabric.Actors;

namespace FG.ServiceFabric.Services.Runtime.StateSession
{
	public interface ISchemaKey
	{
		string Schema { get; }
		string Key { get; }
	}

	public class QueueInfoStateKey : ISchemaKey
	{
		private const string ReliableStateQueueInfoName = @"QUEUEINFO";

		public string Schema { get; private set; }
		public string Key { get; private set; }

		public QueueInfoStateKey(string schema)
		{
			Schema = schema;
			Key = ReliableStateQueueInfoName;
		}
	}


	public class QueueItemStateKeyPrefix
	{
		protected const string ReliableStateQueueItemName = @"QUEUE-";

		public string Schema { get; protected set; }
		public string KeyPrefix { get; protected set; }

		public QueueItemStateKeyPrefix(string schema)
		{
			Schema = schema;
			KeyPrefix = ReliableStateQueueItemName;
		}
	}

	public class QueueItemStateKey : QueueItemStateKeyPrefix, ISchemaKey
	{
		public string Key { get; private set; }
		public long Index { get; protected set; }

		public QueueItemStateKey(string schema, long index) : base(schema)
		{
			Key = $"{ReliableStateQueueItemName}{index}";
			Index = index;
		}
	}

    public class DictionaryStateKey : ISchemaKey
    {
        public DictionaryStateKey(string schema, string key)
        {
            Schema = schema;
            Key = key;
        }

        public string Schema { get; }
        public string Key { get; }
    }

    public class ActorIdStateKey : ActorSchemaKey
	{
		internal const string ActorIdStateSchemaName = @"ACTORID";

		public ActorIdStateKey(ActorId actorId) : base(ActorIdStateSchemaName, GetActorIdSchemaKey(actorId))
		{			
		}

		public static implicit operator ActorIdStateKey(ActorId actorId)
		{
			return new ActorIdStateKey(actorId);
		}

		public static implicit operator ActorId(ActorIdStateKey actorIdStateKey)
		{
			return ActorSchemaKey.TryGetActorIdFromSchemaKey(actorIdStateKey.Key);
		}
	}

	public class ActorStateKey : ActorSchemaKey
	{
		internal const string ActorStateSchemaName = @"ACTORSTATE";

		public ActorStateKey(ActorId actorId, string schema) : base(GetSchemaName(schema), GetActorIdSchemaKey(actorId))
		{
		}

		public static string GetSchemaName(string actorState)
		{
			return $"{ActorStateSchemaName}-{actorState}";
		}

		public static string GetActorStateNameFromStateSchemaName(string stateSchemaName)
		{
			return stateSchemaName.Substring(ActorStateSchemaName.Length + 1);
		}
	}

	public class ActorReminderStateKey : ActorSchemaKey
	{
		private const string ActorReminderSchemaName = @"ACTORREMINDER";

		public string ReminderName { get; private set; }

		public ActorReminderStateKey(ActorId actorId, string reminderName) : base(ActorReminderSchemaName, GetReminderStateName(actorId, reminderName))
		{
			ReminderName = reminderName;
		}

		public static ActorReminderStateKey Parse(string key)
		{
			var actorId = ActorSchemaKey.TryGetActorIdFromSchemaKey(key);
			var actorIdKeyPart = GetActorIdSchemaKey(actorId);

			var reminderName = key.Substring(actorIdKeyPart.Length + 1);

			return new ActorReminderStateKey(actorId, reminderName);
		}

		private static string GetReminderStateName(ActorId actorId, string reminderName)
		{
			return $"{GetActorIdSchemaKey(actorId)}-{reminderName}";
		}
	}

	public class ActorReminderCompletedStateKey : ActorSchemaKey
	{
		internal const string ActorReminderCompletedSchemaName = @"ACTORREMINDERCOMPLETED";

		public ActorReminderCompletedStateKey(ActorId actorId) : base(ActorReminderCompletedSchemaName, GetActorIdSchemaKey(actorId))
		{
		}		
	}

	public class ActorSchemaKey : ISchemaKey
	{
		private static readonly Regex RegexActorIdDetector = new Regex(@"(S{.+})|(G{[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}})|(L{[0-9]+})", RegexOptions.Compiled);

		public string Schema { get; private set; }
		public string Key { get; private set; }

		public ActorSchemaKey(string schema, string key)
		{
			Schema = schema;
			Key = key;
		}

		public static implicit operator ActorId(ActorSchemaKey key)
		{
			return TryGetActorIdFromSchemaKey(key.Key);
		}


		public static string GetActorIdSchemaKey(ActorId actorId)
		{
			var key = actorId.ToString();
			switch (actorId.Kind)
			{
				case ActorIdKind.Long:
					key = $"L{{{actorId.ToString()}}}";
					break;

				case ActorIdKind.Guid:
					key = $"G{{{actorId.GetGuidId()}}}";
					break;

				case ActorIdKind.String:
					key = $"S{{{actorId.GetStringId()}}}";
					break;
			}
			return key;
		}

		public static ActorId TryGetActorIdFromSchemaKey(string schemaKey)
		{
			var match = RegexActorIdDetector.Match(schemaKey);
			if (match.Success)
			{
				var value = match.Value;

				var kind = schemaKey.Substring(0, 1);
				var id = schemaKey.Substring(2, value.Length - 3);

				if (kind.Equals("G", StringComparison.OrdinalIgnoreCase))
				{
					return new ActorId(Guid.Parse(id));
				}

				if (kind.Equals("L", StringComparison.OrdinalIgnoreCase))
				{
					return new ActorId(long.Parse(id));
				}

				if (kind.Equals("S", StringComparison.OrdinalIgnoreCase))
				{
					return new ActorId(id);
				}
			}

			return null;
		}		
	}

	public class SchemaStateKey
	{
		private static readonly Regex RegexActorSchemaStateKeySplitter = new Regex(
			@"(?'service'[\/\:a-zA-Z0-9\-\.]+)_(?'partition'[a-zA-Z0-9\-]+)_(?'schema'[a-zA-Z0-9\-]+)_(?'key'.+)",
			RegexOptions.Compiled);

		public SchemaStateKey(string serviceName, string partitionKey, string schema = null, string key = null)
		{
			ServiceName = serviceName;
			PartitionKey = partitionKey;
			Schema = schema;
			Key = key;
		}

		public string ServiceName { get; private set; }
		public string PartitionKey { get; private set; }
		public string Schema { get; private set; }
		public string Key { get; private set; }

		public static SchemaStateKey Parse(string schemaStateKey)
		{
			var match = RegexActorSchemaStateKeySplitter.Match(schemaStateKey);
			if (!match.Success)
			{
				throw new NotSupportedException($"The key {schemaStateKey} cannot be parsed as a SchemaStateKey");
			}

			var serviceName = match.Groups["service"]?.Value;
			var partititonKey = match.Groups["partition"]?.Value;
			var schema = match.Groups["schema"]?.Value;
			var key = match.Groups["key"]?.Value;

			return new SchemaStateKey(serviceName, partititonKey, schema, key);
		}

		public override string ToString()
		{
			var value = new StringBuilder();
			if (ServiceName != null)
			{
				value = value.Append(ServiceName).Append("_");
				if (PartitionKey != null)
				{
					value = value.Append(PartitionKey).Append("_");
					if (Schema != null)
					{
						value = value.Append(Schema).Append("_");
						if (Key != null)
						{
							value = value.Append(Key);
						}
					}
				}
			}
			return value.ToString();
		}

		public static implicit operator SchemaStateKey(string key)
		{
			return SchemaStateKey.Parse(key);
		}

		public static implicit operator string(SchemaStateKey key)
		{
			return key.ToString();
		}
	}
}