using System;
using System.Threading;
using System.Threading.Tasks;
using FG.ServiceFabric.Tests.DbStoredActor.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;

namespace FG.ServiceFabric.Tests.WebApi.Controllers
{
    [Route("api/[controller]")]
    public class DbStoredActorController : Controller
    {
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var proxy = new ActorProxyFactory().CreateActorProxy<IDbStoredActor>(
                serviceUri: new Uri("fabric:/FG.ServiceFabric.Tests.Application/DbStoredActorService"),
                actorId: new ActorId(id));
            var count = await proxy.GetCountAsync(CancellationToken.None);

            return Ok(count);
        }

        [HttpPost("{id}")]
        public async void Post(Guid id, [FromBody] UICommand value)
        {
            var proxy = new ActorProxyFactory().CreateActorProxy<IDbStoredActor>(new ActorId(id));
            await proxy.SetCountAsync(value.Count, CancellationToken.None);
        }

        // ReSharper disable once InconsistentNaming
        public class UICommand
        {
            public int Count { get; set; }
        }
    }
}