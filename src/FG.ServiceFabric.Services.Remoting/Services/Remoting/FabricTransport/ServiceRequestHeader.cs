using System;

namespace FG.ServiceFabric.Services.Remoting.FabricTransport
{
    public abstract class ServiceRequestHeader
    {
        public string HeaderName { get; set; }
        //public byte[] Value { get; set; }        

        public abstract byte[] GetValue();
    }
}