using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FG.ServiceFabric.Services.Remoting.FabricTransport.Client
{
    internal class ServiceRemotingClientCache
    {
        private readonly TimeSpan _timeToLive;

        public ServiceRemotingClientCache(TimeSpan timeToLive)
        {
            this._timeToLive = timeToLive;
        }

    }
}
