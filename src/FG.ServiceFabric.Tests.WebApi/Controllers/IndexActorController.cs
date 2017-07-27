using System;
using System.Threading.Tasks;
using FG.ServiceFabric.Tests.EventStoredActor.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;

namespace FG.ServiceFabric.Tests.WebApi.Controllers
{
    [Route("api/[controller]")]
    public class IndexActorController : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var index = await new ActorProxyFactory().CreateActorProxy<IIndexActor>(actorId: new ActorId("Index")).ListCommandsAsync();
            return Ok(index);
        }
    }
}