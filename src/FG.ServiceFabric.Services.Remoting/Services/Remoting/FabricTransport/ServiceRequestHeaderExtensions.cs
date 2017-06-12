using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FG.ServiceFabric.Utils;

namespace FG.ServiceFabric.Services.Remoting.FabricTransport
{
    public static class ServiceRequestHeaderExtensions
    {
        public static IEnumerable<ServiceRequestHeader> CreateHeaders(this IDictionary<string, string> headers)
        {
            return headers.Select(CreateHeader).ToArray();
        }

        private static ServiceRequestHeader CreateHeader(KeyValuePair<string, string> header)
        {
            return new NamedServiceRequestHeader(header.Key, header.Value);
        }

        public static ServiceRequestHeader CreateHeader(string name, string value)
        {
            return new NamedServiceRequestHeader(name, value);
        }

        public static string GetString(this ServiceRequestHeader header)
        {
            return Encoding.UTF8.GetString(header.GetValue());
        }

        public static void SetString(this FixedValueServiceRequestHeader header, string value)
        {
            header.Value = Encoding.UTF8.GetBytes(value);
        }

        public static Guid GetGuid(this FixedValueServiceRequestHeader header)
        {
            return new Guid(header.Value);
        }

        public static void SetGuid(this FixedValueServiceRequestHeader header, Guid value)
        {
            header.Value = value.ToByteArray();
        }

        public static int GetInt(this FixedValueServiceRequestHeader header)
        {
            return BitConverter.ToInt32(header.Value, 0);
        }

        public static void SetInt(this FixedValueServiceRequestHeader header, int value)
        {
            header.Value = BitConverter.GetBytes(value);
        }

        public static T GetCustomValue<T>(this FixedValueServiceRequestHeader header)
        {
            var value = header.Value.Deserialize<T>();
            return value;
        }

        public static void SetCustomValue<T>(this FixedValueServiceRequestHeader header, T value)
        {
            header.Value = value.Serialize();
        }
    }
}