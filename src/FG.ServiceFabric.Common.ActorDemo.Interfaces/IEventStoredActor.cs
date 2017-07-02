using System;
using System.Threading.Tasks;
using FG.ServiceFabric.CQRS;
using Microsoft.ServiceFabric.Actors;

namespace FG.ServiceFabric.Tests.Actor.Interfaces
{
    public interface IEventStoredActor : IActor
    {
        Task CreateAsync(MyCommand command);
        Task UpdateAsync(MyCommand command);
    }
    
    public class MyCommand : DomainCommandBase
    {
        public Guid AggretateRootId { get; set; }
        public string Value { get; set; }
    }
}