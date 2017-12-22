namespace FG.ServiceFabric.Services.Remoting.FabricTransport
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;

    using FG.Common.Extensions;
    using FG.Common.Utils;

    using Microsoft.ServiceFabric.Services.Remoting.V1;

    [DataContract(Name = "cstm", Namespace = "urn:serviceaudit")]
    public class CustomServiceRequestHeader : ServiceRequestHeader
    {
        private const string CustomServiceRequestHeaderName = "CustomServiceRequestHeader";

        private byte[] _bytes;

        private Dictionary<string, string> _headers;

        private bool _needsPackaging;

        private bool _needsUnpacking;

        public CustomServiceRequestHeader()
            : base(CustomServiceRequestHeaderName)
        {
            this._headers = new Dictionary<string, string>();

            this._needsUnpacking = false;
            this._needsPackaging = false;
        }

        public CustomServiceRequestHeader(IReadOnlyDictionary<string, string> headers)
            : base(CustomServiceRequestHeaderName)
        {
            this._headers = headers.ToDictionary(h => h.Key, h => h.Value);

            this._needsUnpacking = false;
            this._needsPackaging = true;
        }

        private CustomServiceRequestHeader(byte[] bytes)
            : base(CustomServiceRequestHeaderName)
        {
            this._bytes = bytes;
            this.Unpack();
        }

        public string this[string index] => this.GetHeader(index);

        public static bool TryFromServiceMessageHeaders(ServiceRemotingMessageHeaders headers, out CustomServiceRequestHeader customServiceRequestHeader)
        {
            customServiceRequestHeader = null;
            if (!headers.TryGetHeaderValue(CustomServiceRequestHeaderName, out var headerValue))
            {
                return false;
            }

            customServiceRequestHeader = new CustomServiceRequestHeader(headerValue);
            return true;
        }

        public CustomServiceRequestHeader AddHeader(string name, string value)
        {
            this._headers.Add(name, value);
            this._needsPackaging = true;

            return this;
        }

        public CustomServiceRequestHeader AddHeader(KeyValuePair<string, string> header)
        {
            this._headers.Add(header.Key, header.Value);
            this._needsPackaging = true;

            return this;
        }

        public string GetHeader(string name)
        {
            return this.GetHeaders().GetValueOrDefault(name);
        }

        public IEnumerable<string> GetHeaderNames()
        {
            return this.UnpackIfRequired()._headers.Keys;
        }

        public IDictionary<string, string> GetHeaders()
        {
            return this.UnpackIfRequired()._headers;
        }

        public override byte[] GetValue()
        {
            this.PackIfRequired();

            return this._bytes;
        }

        public ServiceRemotingMessageHeaders ToServiceMessageHeaders()
        {
            if (this._needsPackaging)
            {
                this.Pack();
            }

            var remotingMessageHeaders = new ServiceRemotingMessageHeaders();
            remotingMessageHeaders.AddHeader(CustomServiceRequestHeaderName, this._bytes);
            return remotingMessageHeaders;
        }

        private void Pack()
        {
            this._bytes = this._headers.Serialize();
            this._needsPackaging = false;
        }

        private CustomServiceRequestHeader PackIfRequired()
        {
            if (this._needsPackaging)
            {
                this.Pack();
            }

            return this;
        }

        private void Unpack()
        {
            this._headers = this._bytes.Deserialize<Dictionary<string, string>>();
            this._needsUnpacking = false;
            this._bytes = null;
        }

        private CustomServiceRequestHeader UnpackIfRequired()
        {
            if (this._needsUnpacking)
            {
                this.Unpack();
            }

            return this;
        }
    }
}