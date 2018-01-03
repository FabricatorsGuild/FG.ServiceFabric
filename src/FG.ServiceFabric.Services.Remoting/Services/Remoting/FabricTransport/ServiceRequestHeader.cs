namespace FG.ServiceFabric.Services.Remoting.FabricTransport
{
    public abstract class ServiceRequestHeader
    {
        public ServiceRequestHeader(string headerName)
        {
            this.HeaderName = headerName;
        }

        public string HeaderName { get; set; }

        public abstract byte[] GetValue();
    }
}