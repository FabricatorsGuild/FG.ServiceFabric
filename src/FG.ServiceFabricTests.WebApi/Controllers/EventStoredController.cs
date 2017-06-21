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
    public class EventStoredController : Controller
    {
        // POST api/values
        [HttpPost("{id}")]
        public void Post(string id, [FromBody]Command value)
        {
            new ActorProxyFactory().CreateActorProxy<IEventStoredActor>(new ActorId(id)).RaiseAsync(value.Value);
        }

        public class Command
        {
            public string Value { get; set; }
        }
    }
}
