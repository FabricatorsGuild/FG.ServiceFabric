using System;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Testing.Mocks.Actors.Runtime
{
    public class MockActorTimer : IActorTimer
    {
        public void Dispose()
        {            
        }

        public TimeSpan DueTime { get; set; }
        public TimeSpan Period { get; set; }
    }
}