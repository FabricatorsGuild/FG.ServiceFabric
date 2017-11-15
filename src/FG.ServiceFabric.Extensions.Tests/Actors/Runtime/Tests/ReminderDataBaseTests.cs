using System;
using System.Diagnostics;
using System.Runtime.Serialization;
using FG.ServiceFabric.Actors.Runtime.Reminders;
using FluentAssertions;
using NUnit.Framework;

namespace FG.ServiceFabric.Actors.Runtime.Tests
{
	public class ReminderDataBaseTests
	{
		[Test]
		public void Should_deserialize_serialized_state()
		{
			var value = 5;
			var startedTime = DateTime.Now;
			var data = new SomeState() {Value = value, StartedTime = startedTime}.Serialize();

			var deserialized = SomeState.Deserialize(data);

			deserialized.Value.Should().Be(value);
			deserialized.StartedTime.Should().Be(startedTime);
		}

		[Test]
		public void Test_serialization_types()
		{
			var types = new[]
				{ReminderDataSerializationType.Binary, ReminderDataSerializationType.Xml, ReminderDataSerializationType.Json};

			var timer = new Stopwatch();

			foreach (var type in types)
			{
				timer.Reset();
				timer.Start();
				var length = 0;
				for (int i = 0; i < 10000; i++)
				{
					var someState = new SomeState() {Value = i, StartedTime = DateTime.Now};
					var serialize = someState.Serialize(type);
					length += serialize.Length;
				}
				timer.Stop();

				Console.WriteLine($"{type} length: {length} bytes {timer.ElapsedMilliseconds} ms");
			}
		}

		[Test]
		public void Test_deserialization_types()
		{
			var types = new[]
				{ReminderDataSerializationType.Binary, ReminderDataSerializationType.Xml, ReminderDataSerializationType.Json};

			var timer = new Stopwatch();

			foreach (var type in types)
			{
				timer.Reset();
				timer.Start();
				var length = 0;
				for (int i = 0; i < 10000; i++)
				{
					var someState = new SomeState() {Value = i, StartedTime = DateTime.Now};
					var serialize = someState.Serialize(type);
					length += serialize.Length;

					var deserialized = SomeState.Deserialize(serialize, type);
				}
				timer.Stop();

				Console.WriteLine($"{type} length: {length} bytes {timer.ElapsedMilliseconds} ms");
			}
		}
	}

	[Serializable]
	[DataContract]
	public class SomeState : ReminderDataBase<SomeState>
	{
		[DataMember]
		public long Value { get; set; }

		[DataMember]
		public DateTime StartedTime { get; set; }
	}
}