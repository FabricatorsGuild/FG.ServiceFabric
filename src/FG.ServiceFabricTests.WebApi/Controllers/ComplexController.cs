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
    public class ComplexController : Controller
    {
        // GET api/values/5
        [HttpGet("{id}")]
        public async Task<ComplexType> Get(string id)
        {
            try
            {
                return
                    await new ActorProxyFactory().CreateActorProxy<IComplexActor>(new ActorId(id))
                        .GetComplexTypeAsync();
            }
            catch (Exception e)
            {
                throw;
            }
        }

        // POST api/values
        [HttpPost("{id}")]
        public async Task Post(string id, [FromBody]Command value)
        {
            try
            {
                await new ActorProxyFactory().CreateActorProxy<IComplexActor>(new ActorId(id)).SetComplexTypeAsync(value.Value);
            }
            catch (Exception e)
            {
                throw;
            }
        }

        public class Command
        {
            public string Value { get; set; }
        }
    }
}
