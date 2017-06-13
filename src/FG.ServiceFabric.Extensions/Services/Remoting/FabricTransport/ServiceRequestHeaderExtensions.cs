using System;
using System.Text;
using FG.Common.Utils;

namespace FG.ServiceFabric.Services.Remoting.FabricTransport
{
    public static class ServiceRequestHeaderExtensions
    {
        public static string GetString(this ServiceRequestHeader header)
        {
            return Encoding.UTF8.GetString(header.Value);
        }

        public static void SetString(this ServiceRequestHeader header, string value)
        {
            header.Value = Encoding.UTF8.GetBytes(value);
        }

        public static Guid GetGuid(this ServiceRequestHeader header)
        {
            return new Guid(header.Value);
        }

        public static void SetGuid(this ServiceRequestHeader header, Guid value)
        {
            header.Value = value.ToByteArray();
        }

        public static int GetInt(this ServiceRequestHeader header)
        {
            return BitConverter.ToInt32(header.Value, 0);
        }

        public static void SetInt(this ServiceRequestHeader header, int value)
        {
            header.Value = BitConverter.GetBytes(value);
        }

        public static T GetCustomValue<T>(this ServiceRequestHeader header)
        {
            var value = header.Value.Deserialize<T>();
            return value;
        }

        public static void SetCustomValue<T>(this ServiceRequestHeader header, T value)
        {
            header.Value = value.Serialize();
        }
    }
}