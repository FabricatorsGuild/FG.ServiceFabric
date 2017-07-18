using System;
using System.Threading.Tasks;
using FG.ServiceFabric.Tests.PersonActor.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;

namespace FG.ServiceFabric.Tests.WebApi.Controllers
{
    [Route("api/[controller]")]
    public class PersonController : Controller
    {

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            try
            {
                var person = await new ActorProxyFactory().CreateActorServiceProxy<IPersonActorService>(
                    serviceUri: new Uri("fabric:/FG.ServiceFabric.Tests.Application/PersonActorService"),
                    actorId: new ActorId(id)).GetAsync(id);

                return Ok(person);
            }
            catch (Exception e)
            {
                throw;
            }
        }

        [HttpGet("{id}/history")]
        public async Task<IActionResult> GetHistory(Guid id)
        {
            var history = await new ActorProxyFactory().CreateActorServiceProxy<IPersonActorService>(
                serviceUri: new Uri("fabric:/FG.ServiceFabric.Tests.Application/PersonActorService"), 
                actorId: new ActorId(id)).GetAllEventHistoryAsync(id);

            return Ok(history);
        }

        [HttpPost("{id}")]
        public async void Post(Guid id, [FromBody]UICommand value)
        {
            try
            {
                var proxy = new ActorProxyFactory().CreateActorProxy<IPersonActor>(new ActorId(id));
                await proxy.RegisterAsync(new RegisterCommand {AggretateRootId = id, Name = value.Name});
            }
            catch (Exception e)
            {
                throw;
            }
        }

        [HttpPut("{id}")]
        public async void Put(Guid id, [FromBody]UICommand value)
        {
            try
            {
                await new ActorProxyFactory().CreateActorProxy<IPersonActor>(new ActorId(id)).MarryAsync(new MarryCommand { AggretateRootId = id });
            }
            catch (Exception e)
            {
                throw;
            }
        }

        // ReSharper disable once InconsistentNaming
        public class UICommand
        {
            public string Name { get; set; }
        }
    }
}
