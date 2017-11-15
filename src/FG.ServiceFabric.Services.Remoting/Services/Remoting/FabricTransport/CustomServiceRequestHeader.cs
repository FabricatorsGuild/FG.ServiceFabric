using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using FG.Common.Utils;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.V1;

namespace FG.ServiceFabric.Services.Remoting.FabricTransport
{
	[DataContract(Name = "cstm", Namespace = "urn:serviceaudit")]
	public class CustomServiceRequestHeader : ServiceRequestHeader
	{
		private const string CustomServiceRequestHeaderName = "CustomServiceRequestHeader";
		private byte[] _bytes;
		private Dictionary<string, string> _headers;

		private bool _needsPackaging = false;
		private bool _needsUnpacking = false;

		public CustomServiceRequestHeader()
		{
			HeaderName = CustomServiceRequestHeaderName;
			_headers = new Dictionary<string, string>();

			_needsUnpacking = false;
			_needsPackaging = false;
		}

		public CustomServiceRequestHeader(IDictionary<string, string> headers)
		{
			HeaderName = CustomServiceRequestHeaderName;
			_headers = new Dictionary<string, string>();
			foreach (var header in headers)
			{
				_headers.Add(header.Key, header.Value);
			}

			_needsUnpacking = false;
			_needsPackaging = true;
		}

		private CustomServiceRequestHeader(byte[] bytes)
		{
			HeaderName = CustomServiceRequestHeaderName;
			_bytes = bytes;
			Unpack();
		}

		public string this[string index] => GetHeader(index);

		public CustomServiceRequestHeader AddHeader(string name, string value)
		{
			_headers.Add(name, value);
			_needsPackaging = true;

			return this;
		}

		public CustomServiceRequestHeader AddHeader(KeyValuePair<string, string> header)
		{
			_headers.Add(header.Key, header.Value);
			_needsPackaging = true;

			return this;
		}

		public static bool TryFromServiceMessageHeaders(ServiceRemotingMessageHeaders headers,
			out CustomServiceRequestHeader customServiceRequestHeader)
		{
			customServiceRequestHeader = (CustomServiceRequestHeader) null;
			byte[] headerValue;
			if (!headers.TryGetHeaderValue(CustomServiceRequestHeaderName, out headerValue))
				return false;
			customServiceRequestHeader = new CustomServiceRequestHeader(headerValue);
			return true;
		}

		private void Unpack()
		{
			_headers = _bytes.Deserialize<Dictionary<string, string>>();
			_needsUnpacking = false;
			_bytes = null;
		}

		private void Pack()
		{
			_bytes = _headers.Serialize();
			_needsPackaging = false;
		}

		public IEnumerable<string> GetHeaderNames()
		{
			if (_needsUnpacking)
			{
				Unpack();
			}

			return _headers.Keys;
		}

		public IDictionary<string, string> GetHeaders()
		{
			if (_needsUnpacking)
			{
				Unpack();
			}
			return _headers;
		}

		public string GetHeader(string name)
		{
			if (_needsUnpacking)
			{
				Unpack();
			}
			return _headers.ContainsKey(name) ? _headers[name] : null;
		}

		public ServiceRemotingMessageHeaders ToServiceMessageHeaders()
		{
			if (_needsPackaging)
			{
				Pack();
			}

			var remotingMessageHeaders = new ServiceRemotingMessageHeaders();
			remotingMessageHeaders.AddHeader(CustomServiceRequestHeaderName, _bytes);
			return remotingMessageHeaders;
		}

		public override byte[] GetValue()
		{
			if (_needsPackaging)
			{
				Pack();
			}
			return _bytes;
		}
	}

	public static class CustomServiceRequestHeaderExtensions
	{
		public static CustomServiceRequestHeader GetCustomHeader(this IEnumerable<ServiceRequestHeader> headers)
		{
			return (CustomServiceRequestHeader) headers.FirstOrDefault(h => h is CustomServiceRequestHeader);
		}
	}
}