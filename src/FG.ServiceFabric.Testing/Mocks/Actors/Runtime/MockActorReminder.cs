using System;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Testing.Mocks.Actors.Runtime
{
    public class MockActorReminder : IActorReminder
    {
        public string Name { get; set; }
        public TimeSpan DueTime { get; set; }
        public TimeSpan Period { get; set; }
        public byte[] State { get; set; }
    }
}