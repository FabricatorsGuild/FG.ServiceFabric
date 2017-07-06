using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FG.ServiceFabric.Tests.Actor.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;

namespace FG.ServiceFabricTests.WebApi.Controllers
{
    [Route("api/[controller]")]
    public class TempEventStoredController : Controller
    {
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var person = await new ActorProxyFactory().CreateActorServiceProxy<IEventStoredActorService>(
               serviceUri: new Uri("fabric:/FG.ServiceFabric.Tests.Application/EventStoredActorService"),
               actorId: new ActorId(id)).GetAsync(id);

            return Ok(person);
        }

        [HttpGet("{id}/history")]
        public async Task<IActionResult> GetHistory(Guid id)
        {
            var history = await new ActorProxyFactory().CreateActorServiceProxy<IEventStoredActorService>(
                serviceUri: new Uri("fabric:/FG.ServiceFabric.Tests.Application/EventStoredActorService"),
                actorId: new ActorId(id)).GetAllEventHistoryAsync(id);

            return Ok(history);
        }

        [HttpPost("{id}")]
        public async void Post(Guid id, [FromBody]UICommand value)
        {
            await new ActorProxyFactory().CreateActorProxy<ITempEventStoredActor>(new ActorId(id)).GiveBirthAsync(new RegisterCommand { AggretateRootId = id, Name = value.Name });
        }

        [HttpPut("{id}")]
        public async void Put(Guid id, [FromBody]UICommand value)
        {
            await new ActorProxyFactory().CreateActorProxy<ITempEventStoredActor>(new ActorId(id)).MarryAsync(new MarryCommand { AggretateRootId = id});
        }

        public class UICommand
        {
            public string Name { get; set; }
        }
    }
}
