using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FG.ServiceFabric.DocumentDb.Testing;
using FG.ServiceFabric.Tests.DbStoredActor.Interfaces;
using FluentAssertions;
using Microsoft.ServiceFabric.Actors;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NUnit.Framework;

// ReSharper disable InconsistentNaming

namespace FG.ServiceFabric.Tests.Persistance
{
    public class When_state_is_updated : TestBase
    {
        private InMemoryStateSession _inMemoryStateSession;

        [SetUp]
        public async Task SetCountTo3()
        {
            var proxy = ActorProxyFactory.CreateActorProxy<IDbStoredActor>(ActorId.CreateRandom());
            await proxy.SetCountAsync(3, CancellationToken.None);
        }

        protected override void SetupRuntime()
        {
            _inMemoryStateSession = new InMemoryStateSession();
            ForTestDbStoredActor.Setup(_fabricApplication, _inMemoryStateSession);
        }

        [Test]
        public async Task Then_state_is_written_to_document_db()
        {
            var state = await _inMemoryStateSession.QueryAsync<CountState>();
            state.Single().Count.Should().Be(3);
        }
    }

    public class When_statename_is_an_urn
    {
        [Test]
        public void name_should_be_uri_path_segment()
        {
            var stateName = new Uri("urn:myDictionary");

            stateName.AbsolutePath.Should().Be("myDictionary");
        }
    }

    public class When_serializing_inner_object
    {
        [Test]
        public void value_should_be_unescaped_json()
        {
            var value = new TestObject
            {
                OtherValue = 5,
                Value = "Hello"
            };
            var externalStateForTest = new ExternalStateForTest
            {
                Key = @"MyState",
                StateCLRType = typeof(TestObject).AssemblyQualifiedName,
                Value = value
            };

            var serializeObject = JsonConvert.SerializeObject(externalStateForTest);

            var deserializedObject = JsonConvert.DeserializeObject<ExternalStateForTest>(serializeObject,
                new JsonSerializerSettings
                {
                    ContractResolver = new ExternalStateForTestContractResolver()
                    //Converters = new JsonConverter[] {new DummyConvert(),}.ToList()
                });

            deserializedObject.Value.Should().BeOfType<TestObject>();
        }
    }

    public class When_incrementing_queue_indices
    {
        [Test]
        public void should_loop_over_max_value()
        {
            var tail = long.MaxValue - 5;
            var head = tail;
            var longs = new List<long>();
            for (var i = 0; i < 10; i++)
            {
                var index = head + 1;

                longs.Add(index);

                var letter = ((char) (65 + i)).ToString();
                var ordinal = index - tail;
                Console.WriteLine($"{ordinal} {index} - {letter}");

                head = index;
            }

            foreach (var @long in longs.OrderBy(i => i))
                Console.WriteLine(@long);
        }
    }

    public class DummyConvert : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(object);
        }
    }


    public class ExternalStateForTestContractResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            return base.CreateProperty(member, memberSerialization);
        }

        public override JsonContract ResolveContract(Type type)
        {
            return base.ResolveContract(type);
        }

        protected override JsonConverter ResolveContractConverter(Type objectType)
        {
            return base.ResolveContractConverter(objectType);
        }
    }

    public class ExternalStateForTest
    {
        public string Key { get; set; }
        public string StateCLRType { get; set; }

        [JsonProperty(TypeNameHandling = TypeNameHandling.Objects)]
        public object Value { get; set; }
    }

    public class TestObject
    {
        public string Value { get; set; }
        public int OtherValue { get; set; }
    }
}