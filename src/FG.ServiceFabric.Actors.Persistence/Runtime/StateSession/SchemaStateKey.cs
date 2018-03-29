using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.ServiceFabric.Actors;

namespace FG.ServiceFabric.Services.Runtime.StateSession
{
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
            return TryGetActorIdFromSchemaKey(actorIdStateKey.Key);
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
        internal const string ActorReminderSchemaName = @"ACTORREMINDER";

        public ActorReminderStateKey(ActorId actorId, string reminderName) : base(ActorReminderSchemaName,
            GetReminderStateName(actorId, reminderName))
        {
            ReminderName = reminderName;
        }

        public string ReminderName { get; }

        public static ActorReminderStateKey Parse(string key)
        {
            var actorId = TryGetActorIdFromSchemaKey(key);
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

        public ActorReminderCompletedStateKey(ActorId actorId) : base(ActorReminderCompletedSchemaName,
            GetActorIdSchemaKey(actorId))
        {
        }
    }

    public class ActorSchemaKey : ISchemaKey
    {
        private static readonly Regex RegexActorIdDetector =
            new Regex(
                @"(S{.+})|(G{[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}})|(L{[0-9]+})",
                RegexOptions.Compiled);

        public ActorSchemaKey(string schema, string key)
        {
            Schema = schema;
            Key = key;
        }

        public string Schema { get; }
        public string Key { get; }

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
                    key = $"L{{{actorId}}}";
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
                    return new ActorId(Guid.Parse(id));

                if (kind.Equals("L", StringComparison.OrdinalIgnoreCase))
                    return new ActorId(long.Parse(id));

                if (kind.Equals("S", StringComparison.OrdinalIgnoreCase))
                    return new ActorId(id);
            }

            return null;
        }
    }
}