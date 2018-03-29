// ReSharper disable StyleCop.SA1126

using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using FG.Common.Extensions;
using FG.Common.Utils;
using Microsoft.ServiceFabric.Services.Remoting.V1;

namespace FG.ServiceFabric.Services.Remoting.FabricTransport
{
    /// <summary>
    ///     Provides a custom service request header
    /// </summary>
    [DataContract(Name = "cstm", Namespace = "urn:serviceaudit")]
    public class CustomServiceRequestHeader : ServiceRequestHeader
    {
        private const string CustomServiceRequestHeaderName = "CustomServiceRequestHeader";

        private byte[] _bytes;

        private Dictionary<string, string> _headers;

        private bool _needsPackaging;

        private bool _needsUnpacking;

        /// <summary>
        ///     Initializes a new instance of the <see cref="CustomServiceRequestHeader" /> class.
        /// </summary>
        public CustomServiceRequestHeader()
            : base(CustomServiceRequestHeaderName)
        {
            _headers = new Dictionary<string, string>();

            _needsUnpacking = false;
            _needsPackaging = false;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="CustomServiceRequestHeader" /> class.
        /// </summary>
        /// <param name="headers">
        ///     The headers
        /// </param>
        public CustomServiceRequestHeader(IEnumerable<KeyValuePair<string, string>> headers)
            : base(CustomServiceRequestHeaderName)
        {
            _headers = headers.ToDictionary(h => h.Key, h => h.Value);

            _needsUnpacking = false;
            _needsPackaging = true;
        }

        private CustomServiceRequestHeader(byte[] bytes)
            : base(CustomServiceRequestHeaderName)
        {
            _bytes = bytes;
            Unpack();
        }

        /// <summary>
        ///     Gets a header by name/key
        /// </summary>
        /// <param name="key">The header name/key</param>
        /// <returns>A string containing the value of the property or null if the property does not exist or is null</returns>
        public string this[string key] => GetHeader(key);

        /// <summary>
        ///     Tries to create a <see cref="CustomServiceRequestHeader" /> containing all properties
        /// </summary>
        /// <param name="headers">The headers</param>
        /// <param name="customServiceRequestHeader">Th custom service request header</param>
        /// <returns>True if the headers exist, false if it does not exist</returns>
        public static bool TryFromServiceMessageHeaders(ServiceRemotingMessageHeaders headers,
            out CustomServiceRequestHeader customServiceRequestHeader)
        {
            customServiceRequestHeader = null;
            if (!headers.TryGetHeaderValue(CustomServiceRequestHeaderName, out var headerValue))
                return false;

            customServiceRequestHeader = new CustomServiceRequestHeader(headerValue);
            return true;
        }

        /// <summary>
        ///     Adds a header value
        /// </summary>
        /// <param name="name">The property key/name</param>
        /// <param name="value">The property valuke</param>
        /// <returns>A <see cref="CustomServiceRequestHeader" /></returns>
        public CustomServiceRequestHeader AddHeader(string name, string value)
        {
            _headers.Add(name, value);
            _needsPackaging = true;

            return this;
        }

        /// <summary>
        ///     Adds a header value
        /// </summary>
        /// <param name="header">The property key/name &amp; valye pair</param>
        /// <returns>A <see cref="CustomServiceRequestHeader" /></returns>
        public CustomServiceRequestHeader AddHeader(KeyValuePair<string, string> header)
        {
            _headers.Add(header.Key, header.Value);
            _needsPackaging = true;

            return this;
        }

        /// <summary>
        ///     Gets a header by name
        /// </summary>
        /// <param name="name">The header name</param>
        /// <returns>The header value or null if it does not exist or has the value null</returns>
        public string GetHeader(string name)
        {
            return GetHeaders().GetValueOrDefault(name);
        }

        /// <summary>
        ///     Gets all header names
        /// </summary>
        /// <returns>The header names</returns>
        public IEnumerable<string> GetHeaderNames()
        {
            return UnpackIfRequired()._headers.Keys;
        }

        /// <summary>
        ///     Gets all headers
        /// </summary>
        /// <returns>The headers</returns>
        public IDictionary<string, string> GetHeaders()
        {
            return UnpackIfRequired()._headers;
        }

        /// <summary>
        ///     Gets the instance's packed value
        /// </summary>
        /// <returns>The packed value</returns>
        public override byte[] GetValue()
        {
            PackIfRequired();

            return _bytes;
        }

        /// <summary>
        ///     Create a new <see cref="ServiceRemotingMessageHeaders" /> from this instance
        /// </summary>
        /// <returns>The new <see cref="ServiceRemotingMessageHeaders" /></returns>
        public ServiceRemotingMessageHeaders ToServiceMessageHeaders()
        {
            if (_needsPackaging)
                Pack();

            var remotingMessageHeaders = new ServiceRemotingMessageHeaders();
            remotingMessageHeaders.AddHeader(CustomServiceRequestHeaderName, _bytes);
            return remotingMessageHeaders;
        }

        /// <summary>
        ///     Packs the current header
        /// </summary>
        private void Pack()
        {
            _bytes = _headers.Serialize();
            _needsPackaging = false;
        }

        /// <summary>
        ///     Packs the current header if it's required
        /// </summary>
        /// <returns>The new <see cref="CustomServiceRequestHeader" /></returns>
        private CustomServiceRequestHeader PackIfRequired()
        {
            if (_needsPackaging)
                Pack();

            return this;
        }

        private void Unpack()
        {
            _headers = _bytes.Deserialize<Dictionary<string, string>>();
            _needsUnpacking = false;
            _bytes = null;
        }

        private CustomServiceRequestHeader UnpackIfRequired()
        {
            if (_needsUnpacking)
                Unpack();

            return this;
        }
    }
}