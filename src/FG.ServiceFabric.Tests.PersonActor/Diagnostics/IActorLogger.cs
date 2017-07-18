using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FG.ServiceFabric.CQRS.ReliableMessaging;

namespace FG.ServiceFabric.Tests.PersonActor.Diagnostics
{
    public interface IActorLogger : IReliableMessageChannelLogger
    {
    }
}
