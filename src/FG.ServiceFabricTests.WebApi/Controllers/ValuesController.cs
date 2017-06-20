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
    public class ValuesController : Controller
    {
        // GET api/values/5
        [HttpGet("{id}")]
        public async Task<ComplexType> Get(string id)
        {
            return
                await new ActorProxyFactory().CreateActorProxy<IActorDemo>(new ActorId(id))
                    .GetComplexTypeAsync();
        }

        // POST api/values
        [HttpPost("{id}")]
        public void Post(string id, [FromBody]Command value)
        {
            new ActorProxyFactory().CreateActorProxy<IActorDemo>(new ActorId(id)).SetComplexTypeAsync(value.Value);
        }

        public class Command
        {
            public string Value { get; set; }
        }
    }
}
