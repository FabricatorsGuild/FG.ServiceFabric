using System;
using FG.ServiceFabric.Tests.Actor.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;

namespace FG.ServiceFabricTests.WebApi.Controllers
{
    [Route("api/[controller]")]
    public class EventStoredController : Controller
    {
        // POST api/values
        [HttpPost("{id}")]
        public async void Post(Guid id, [FromBody]Command value)
        {
            await new ActorProxyFactory().CreateActorProxy<IEventStoredActor>(new ActorId(id)).CreateAsync(new MyCommand {AggretateRootId = id, Value = value.Value});
        }

        [HttpPut("{id}")]
        public async void Put(Guid id, [FromBody]Command value)
        {
            await new ActorProxyFactory().CreateActorProxy<IEventStoredActor>(new ActorId(id)).UpdateAsync(new MyCommand { AggretateRootId = id, Value = value.Value });
        }

        public class Command
        {
            public string Value { get; set; }
        }
    }
}
