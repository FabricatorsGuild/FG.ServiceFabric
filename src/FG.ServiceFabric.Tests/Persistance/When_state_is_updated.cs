using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FG.ServiceFabric.DocumentDb.Testing;
using FG.ServiceFabric.Tests.DbStoredActor.Interfaces;
using FluentAssertions;
using Microsoft.ServiceFabric.Actors;
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
           ForTestDbStoredActor.Setup(FabricRuntime, _inMemoryStateSession);
        }

        [Test]
        public async Task Then_state_is_written_to_document_db()
        {
            var state = await _inMemoryStateSession.QueryAsync<CountState>();
            state.Single().Count.Should().Be(3);
        }
    }
}
