using System;
using FG.ServiceFabric.Services.Runtime.StateSession;
using FluentAssertions;
using NUnit.Framework;

namespace FG.ServiceFabric.Tests.Persistence
{
    // ReSharper disable InconsistentNaming
    public class With_SchemaStateKey
    {
        [Test]
        public void Should_build_full_dictionary_key()
        {
            var service = "Core-PCSyncCompanyChannelAdapter";
            var range = "singleton";
            var schema = "error_dictionary";
            var key = "48a46df3-843e-4645-b8de-075259c826f2";

            var dictionaryStateKey = new DictionaryStateKey(schema, key);
            var schemaStateKey =
                new SchemaStateKey(service, range, dictionaryStateKey?.Schema, dictionaryStateKey?.Key);

            schemaStateKey.ServiceName.Should().Be(service);
            schemaStateKey.PartitionKey.Should().Be(range);
            schemaStateKey.Schema.Should().Be(schema);
            schemaStateKey.Key.Should().Be(key);
            schemaStateKey.ToString().Should()
                .Be("Core-PCSyncCompanyChannelAdapter_singleton_error_dictionary_48a46df3-843e-4645-b8de-075259c826f2");
        }

        [Test]
        public void Should_build_dictionary_key_prefix()
        {
            var service = "Core-PCSyncCompanyChannelAdapter";
            var range = "singleton";
            var schema = "error_dictionary";

            var dictionaryStateKey = new DictionaryStateKey(schema, null);
            var schemaStateKey =
                new SchemaStateKey(service, range, dictionaryStateKey?.Schema, dictionaryStateKey?.Key);

            schemaStateKey.ServiceName.Should().Be(service);
            schemaStateKey.PartitionKey.Should().Be(range);
            schemaStateKey.Schema.Should().Be(schema);
            schemaStateKey.Key.Should().Be(null);
            schemaStateKey.ToString().Should().Be("Core-PCSyncCompanyChannelAdapter_singleton_error_dictionary_");
        }

        [Test]
        public void Should_build_empty_dictionary_key_prefix()
        {
            var service = "Core-PCSyncCompanyChannelAdapter";
            var range = "singleton";

            var dictionaryStateKey = new DictionaryStateKey(null, null);
            var schemaStateKey =
                new SchemaStateKey(service, range, dictionaryStateKey?.Schema, dictionaryStateKey?.Key);

            schemaStateKey.ServiceName.Should().Be(service);
            schemaStateKey.PartitionKey.Should().Be(range);
            schemaStateKey.Schema.Should().Be(null);
            schemaStateKey.Key.Should().Be(null);
            schemaStateKey.ToString().Should().Be("Core-PCSyncCompanyChannelAdapter_singleton_");
        }

        [Test]
        public void Should_build_queue_info_key()
        {
            var service = "Core-PCSyncCompanyChannelAdapter";
            var range = "singleton";
            var schema = "error_queue";

            var queueStateKey = new QueueInfoStateKey(schema);
            var schemaStateKey = new SchemaStateKey(service, range, queueStateKey?.Schema, queueStateKey?.Key);

            schemaStateKey.ServiceName.Should().Be(service);
            schemaStateKey.PartitionKey.Should().Be(range);
            schemaStateKey.Schema.Should().Be(schema);
            schemaStateKey.Key.Should().Be("QUEUEINFO");
            schemaStateKey.ToString().Should().Be("Core-PCSyncCompanyChannelAdapter_singleton_error_queue_QUEUEINFO");
        }

        [Test]
        public void Should_build_full_queue_item_key()
        {
            var service = "Core-PCSyncCompanyChannelAdapter";
            var range = "singleton";
            var schema = "error_queue";
            var index = 100L;

            var queueStateKey = new QueueItemStateKey(schema, index);
            var schemaStateKey = new SchemaStateKey(service, range, queueStateKey?.Schema, queueStateKey?.Key);

            schemaStateKey.ServiceName.Should().Be(service);
            schemaStateKey.PartitionKey.Should().Be(range);
            schemaStateKey.Schema.Should().Be(schema);
            schemaStateKey.Key.Should().Be("QUEUE-100");
            queueStateKey.Index.Should().Be(100L);
            schemaStateKey.ToString().Should().Be("Core-PCSyncCompanyChannelAdapter_singleton_error_queue_QUEUE-100");
        }

        [Test]
        public void Should_build_queue_item_prefix()
        {
            var service = "Core-PCSyncCompanyChannelAdapter";
            var range = "singleton";
            var schema = "error_queue";

            var queueStateKey = new QueueItemStateKeyPrefix(schema);
            var schemaStateKey = new SchemaStateKey(service, range, queueStateKey?.Schema, queueStateKey?.KeyPrefix);

            schemaStateKey.ServiceName.Should().Be(service);
            schemaStateKey.PartitionKey.Should().Be(range);
            schemaStateKey.Schema.Should().Be(schema);
            schemaStateKey.Key.Should().Be("QUEUE-");
            schemaStateKey.ToString().Should().Be("Core-PCSyncCompanyChannelAdapter_singleton_error_queue_QUEUE-");
        }


        [Test]
        public void Should_build_ServiceName_with_fabric_schema()
        {
            // fabric:/Overlord/StatefulServiceDemo_range-0_myDictionary2_theValue
            var id = "fabric:/Overlord/StatefulServiceDemo_range-0_myDictionary2_theValue";

            var service = "fabric:/Overlord/StatefulServiceDemo";
            var range = "range-0";
            var schema = "myDictionary2";
            var key = "theValue";

            var schemaStateKey = SchemaStateKey.Parse(id);

            schemaStateKey.ServiceName.Should().Be(service);
            schemaStateKey.PartitionKey.Should().Be(range);
            schemaStateKey.Schema.Should().Be(schema);
            schemaStateKey.Key.Should().Be(key);

            var idOutput = (string) schemaStateKey;
            idOutput.Should().Be(id);

            Console.Write(
                $"{id} => '{schemaStateKey.ServiceName}', '{schemaStateKey.PartitionKey}', '{schemaStateKey.Schema}', '{schemaStateKey.Key}'");
        }

        [Test]
        public void Should_parse_ServiceName_with_fabric_schema()
        {
            // fabric:/Overlord/StatefulServiceDemo_range-0_myDictionary2_theValue
            var id = "fabric:/Overlord/StatefulServiceDemo_range-0_myDictionary2_theValue";

            var service = "fabric:/Overlord/StatefulServiceDemo";
            var range = "range-0";
            var schema = "myDictionary2";
            var key = "theValue";

            var schemaStateKey = new SchemaStateKey(service, range, schema, key);

            schemaStateKey.ServiceName.Should().Be(service);
            schemaStateKey.PartitionKey.Should().Be(range);
            schemaStateKey.Schema.Should().Be(schema);
            schemaStateKey.Key.Should().Be(key);

            var idOutput = (string) schemaStateKey;
            idOutput.Should().Be(id);

            Console.Write(
                $"{id} => '{schemaStateKey.ServiceName}', '{schemaStateKey.PartitionKey}', '{schemaStateKey.Schema}', '{schemaStateKey.Key}'");
        }


        [Test]
        public void Should_parase_ACTORSTATE_id_as_stateschemkey()
        {
            var key =
                @"Broker-TaskActorService_range-0_ACTORSTATE-state_S{helloworld}";

            var schemaStateKey = SchemaStateKey.Parse(key);

            schemaStateKey.PartitionKey.Should().Be("range-0");
            schemaStateKey.ServiceName.Should().Be("Broker-TaskActorService");
            schemaStateKey.Schema.Should().Be("ACTORSTATE-state");
            schemaStateKey.Key.Should().Be("S{helloworld}");
        }

        [Test]
        public void Should_parase_ACTORSTATE_id_with_guids_in_id_as_stateschemkey()
        {
            var key =
                @"Broker-TaskActorService_range-0_ACTORSTATE-state_S{fb1629af-bb0f-40bd-b112-cd5080d38adb-f8d57d54-52fa-4d49-977d-c55e4c94ca30-AgreementDeny}";

            var schemaStateKey = SchemaStateKey.Parse(key);

            schemaStateKey.PartitionKey.Should().Be("range-0");
            schemaStateKey.ServiceName.Should().Be("Broker-TaskActorService");
            schemaStateKey.Schema.Should().Be("ACTORSTATE-state");
            schemaStateKey.Key.Should()
                .Be("S{fb1629af-bb0f-40bd-b112-cd5080d38adb-f8d57d54-52fa-4d49-977d-c55e4c94ca30-AgreementDeny}");
        }

        [Test]
        public void Should_handle_full_keys_with_underscore()
        {
            // Core-PCSyncCompanyChannelAdapter_singleton_error_dictionary_48a46df3-843e-4645-b8de-075259c826f2

            var service = "Core-PCSyncCompanyChannelAdapter";
            var range = "singleton";
            var schema = "error_dictionary";
            var key = "48a46df3-843e-4645-b8de-075259c826f2";

            var schemaStateKey = new SchemaStateKey(service, range, schema, key);

            schemaStateKey.ServiceName.Should().Be(service);
            schemaStateKey.PartitionKey.Should().Be(range);
            schemaStateKey.Schema.Should().Be(schema);
            schemaStateKey.Key.Should().Be(key);

            Console.Write(
                $"{schemaStateKey} => '{schemaStateKey.ServiceName}', '{schemaStateKey.PartitionKey}', '{schemaStateKey.Schema}', '{schemaStateKey.Key}'");
        }

        [Test]
        public void Should_handle_only_schema_keys_with_underscore()
        {
            // Core-PCSyncCompanyChannelAdapter_singleton_error_dictionary_48a46df3-843e-4645-b8de-075259c826f2

            var service = "Core-PCSyncCompanyChannelAdapter";
            var range = "singleton";
            var schema = "error_dictionary";

            var schemaStateKey = new SchemaStateKey(service, range, schema);

            schemaStateKey.ServiceName.Should().Be(service);
            schemaStateKey.PartitionKey.Should().Be(range);
            schemaStateKey.Schema.Should().Be(schema);

            Console.Write(
                $"{schemaStateKey} => '{schemaStateKey.ServiceName}', '{schemaStateKey.PartitionKey}', '{schemaStateKey.Schema}', '{schemaStateKey.Key}'");
        }

        [Test]
        public void Should_handle_only_service_and_partition()
        {
            // Core-PCSyncCompanyChannelAdapter_singleton_error_dictionary_48a46df3-843e-4645-b8de-075259c826f2

            var service = "Core-PCSyncCompanyChannelAdapter";
            var range = "singleton";

            var schemaStateKey = new SchemaStateKey(service, range);

            schemaStateKey.ServiceName.Should().Be(service);
            schemaStateKey.PartitionKey.Should().Be(range);

            Console.Write(
                $"{schemaStateKey} => '{schemaStateKey.ServiceName}', '{schemaStateKey.PartitionKey}', '{schemaStateKey.Schema}', '{schemaStateKey.Key}'");
        }
    }

    // ReSharper restore InconsistentNaming
}