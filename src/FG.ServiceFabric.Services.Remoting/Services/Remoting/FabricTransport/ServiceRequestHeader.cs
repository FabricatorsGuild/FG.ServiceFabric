using System;

namespace FG.ServiceFabric.Services.Remoting.FabricTransport
{
    public class NamedServiceRequestHeader : FixedValueServiceRequestHeader
    {
		public NamedServiceRequestHeader(string name, string value)
		{
			HeaderName = $"x-{name}";
			this.SetString(value);
		}
	}

    public abstract class ServiceRequestHeader
    {
        public string HeaderName { get; set; }
        //public byte[] Value { get; set; }        

        public abstract byte[] GetValue();
    }

    public abstract class FixedValueServiceRequestHeader : ServiceRequestHeader
    {
        public byte[] Value { get; set; }

        public override byte[] GetValue()
        {
            return Value;
        }
    }

    public class UserServiceRequestHeader : FixedValueServiceRequestHeader
    {
        private const string _headerName = @"x-user-name";

        public UserServiceRequestHeader()
        {
            HeaderName = _headerName;
            this.Value = new byte[0];
        }

        public UserServiceRequestHeader(string value)
        {
            HeaderName = _headerName;
            this.SetString(value);
        }
    }


    public class CorreleationIdServiceRequestHeader : FixedValueServiceRequestHeader
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