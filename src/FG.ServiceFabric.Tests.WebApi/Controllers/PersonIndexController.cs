using System;
using System.Threading.Tasks;
using FG.ServiceFabric.Tests.PersonActor.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;

namespace FG.ServiceFabric.Tests.WebApi.Controllers
{
    [Route("api/[controller]")]
    public class PersonIndexController : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var index = await new ActorProxyFactory().CreateActorProxy<IPersonIndexActor>(actorId: new ActorId("PersonIndex")).ListCommandsAsync();
            return Ok(index);
        }
    }
}