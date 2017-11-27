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
		public void Should_parase_ACTORSTATE_id_as_stateschemkey()
		{
			var key =
				@"Broker-TaskActorService_range-0_ACTORSTATE-state_S{helloworld}";

			var schemaStateKey = StateSessionHelper.SchemaStateKey.Parse(key);

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

			var schemaStateKey = StateSessionHelper.SchemaStateKey.Parse(key);

			schemaStateKey.PartitionKey.Should().Be("range-0");
			schemaStateKey.ServiceName.Should().Be("Broker-TaskActorService");
			schemaStateKey.Schema.Should().Be("ACTORSTATE-state");
			schemaStateKey.Key.Should().Be("S{fb1629af-bb0f-40bd-b112-cd5080d38adb-f8d57d54-52fa-4d49-977d-c55e4c94ca30-AgreementDeny}");
		}
	}

	// ReSharper restore InconsistentNaming
}