using System;

namespace FG.ServiceFabric.Services.Remoting.FabricTransport
{
    public abstract class ServiceRequestHeader
    {
        public string HeaderName { get; set; }
        public byte[] Value { get; set; }        
    }

    public class UserServiceRequestHeader : ServiceRequestHeader
    {
        private const string _headerName = @"x-user-name";

        public UserServiceRequestHeader()
        {
            HeaderName = _headerName;
            Value = new byte[0];
        }
        public UserServiceRequestHeader(string value)
        {
            HeaderName = _headerName;
            this.SetString(value);
        }
    }


    public class CorreleationIdServiceRequestHeader : ServiceRequestHeader
    {
        private const string _headerName = @"x-correlation-id";

        public CorreleationIdServiceRequestHeader()
        {
            HeaderName = _headerName;
            this.SetGuid(Guid.NewGuid());
        }
        public CorreleationIdServiceRequestHeader(Guid correlationId)
        {
            HeaderName = _headerName;
            this.SetGuid(correlationId);
        }
    }
}