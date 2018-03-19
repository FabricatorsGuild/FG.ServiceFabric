using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public QueueInfoStateKey(string schema)
        {
            Schema = schema;
            Key = ReliableStateQueueInfoName;
        }

        public string Schema { get; }
        public string Key { get; }
    }


    public class QueueItemStateKeyPrefix
    {
        protected const string ReliableStateQueueItemName = @"QUEUE-";

        public QueueItemStateKeyPrefix(string schema)
        {
            Schema = schema;
            KeyPrefix = ReliableStateQueueItemName;
        }

        public string Schema { get; protected set; }
        public string KeyPrefix { get; protected set; }
    }

    public class QueueItemStateKey : QueueItemStateKeyPrefix, ISchemaKey
    {
        public QueueItemStateKey(string schema, long index) : base(schema)
        {
            Key = $"{ReliableStateQueueItemName}{index}";
            Index = index;
        }

        public long Index { get; protected set; }
        public string Key { get; }
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
    
    public class SchemaStateKey
    {
        public const string Delimiter = "_";
        private const string DelimiterEscaped = @"_";

        private static readonly Regex IllegalCharsReplacer = new Regex($@"['|\\/{DelimiterEscaped}]", RegexOptions.Compiled);
        private static readonly Regex IllegalCharsWithoutDelimiterReplacer = new Regex($@"['|\\/]", RegexOptions.Compiled);
        private static readonly Regex ReplacedCharsReplacer = new Regex(@"'(\d{1,4})'", RegexOptions.Compiled);
        private static readonly Regex RegexActorSchemaStateKeySplitter = new Regex(
            $@"(?'service'[\/\:a-zA-Z0-9\-\.\']+){DelimiterEscaped}(?'partition'[a-zA-Z0-9\-\']+){DelimiterEscaped}(?'schema'[a-zA-Z0-9\-\']+){DelimiterEscaped}(?'key'.+)",
            RegexOptions.Compiled);

        public SchemaStateKey(IdWrapper stateWrapper)
            :this(stateWrapper.ServiceName, stateWrapper.ServicePartitionKey, stateWrapper.Schema, stateWrapper.Key)
        {
        }

        public SchemaStateKey(string serviceName, string servicePartitionKey, string schema = null, string key = null)
        {
            _serviceNameExcaped = EscapeComponent(serviceName);
            _servicePartitionKeyExcaped = EscapeComponent(servicePartitionKey);
            _schemaExcaped = EscapeComponent(schema);
            _keyExcaped = EscapeComponent(key, includeDelimiter:false);

            ServiceName = serviceName;
            ServicePartitionKey = servicePartitionKey;
            Schema = schema;
            Key = key;
        }

        private static string EscapeComponent(string component, bool includeDelimiter = true)
        {
            return component != null ? (includeDelimiter ? IllegalCharsReplacer : IllegalCharsWithoutDelimiterReplacer).Replace(component, ReplaceIllegalChar) : null;
        }

        private static string UnescapeComponent(string component)
        {
            return component  != null ? ReplacedCharsReplacer.Replace(component, RestoreIllegalChar) : null;
        }

        private static string ReplaceIllegalChar(Match m)
        {
            return $"'{(int)m.Value[0]}'";
        }

        private static string RestoreIllegalChar(Match m)
        {
            return int.TryParse(m.Groups[1].Value, out var matchedCharCode) ? $"{(char) matchedCharCode}" : m.Value;
        }

        private readonly string _serviceNameExcaped;
        private readonly string _servicePartitionKeyExcaped;
        private readonly string _schemaExcaped;
        private readonly string _keyExcaped;
        public string ServiceName { get; }
        public string ServicePartitionKey { get; }
        public string Schema { get; }
        public string Key { get; }

        public static SchemaStateKey Parse(string schemaStateKey)
        {
            var match = RegexActorSchemaStateKeySplitter.Match(schemaStateKey);
            if (!match.Success)
                throw new NotSupportedException($"The key {schemaStateKey} cannot be parsed as a SchemaStateKey");

            var serviceName = UnescapeComponent(match.Groups["service"]?.Value);
            var partititonKey = UnescapeComponent(match.Groups["partition"]?.Value);
            var schema = UnescapeComponent(match.Groups["schema"]?.Value);
            var key = UnescapeComponent(match.Groups["key"]?.Value);

            return new SchemaStateKey(serviceName, partititonKey, schema, key);
        }

        public string GetId()
        {
            var value = new StringBuilder();
            if (_serviceNameExcaped != null)
            {
                value = value.Append(_serviceNameExcaped).Append(Delimiter);
                if (_servicePartitionKeyExcaped != null)
                {
                    value = value.Append(_servicePartitionKeyExcaped).Append(Delimiter);
                    if (_schemaExcaped != null)
                    {
                        value = value.Append(_schemaExcaped).Append(Delimiter);
                        if (_keyExcaped != null)
                            value = value.Append(_keyExcaped);
                    }
                }
            }
            return value.ToString();
        }

        public override string ToString()
        {
            return GetId();
        }
    }
}