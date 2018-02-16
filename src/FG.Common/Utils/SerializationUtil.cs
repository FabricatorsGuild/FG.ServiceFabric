using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.Serialization;

namespace FG.Common.Utils
{

    public static class SerializationUtil<T>
    {
        private static readonly DataContractSerializer serializer = new DataContractSerializer(typeof(T));

        public static byte[] Serialize(T value)
        {
            var stream = new MemoryStream();
            serializer.WriteObject(stream, value);
            stream.Position = 0;
            return stream.ToArray();
        }

        public static T Deserialize(byte[] value)
        {
            var stream = new MemoryStream(value) { Position = 0 };
            return (T)serializer.ReadObject(stream);
        }
    }

    public static class SerializationUtil
    {
        private static readonly ConcurrentDictionary<Type, DataContractSerializer> serializers = new ConcurrentDictionary<Type, DataContractSerializer>();

        public static byte[] Serialize<T>(this T value)
        {
            return SerializationUtil<T>.Serialize(value);
        }

        public static T Deserialize<T>(this byte[] value)
        {
            return SerializationUtil<T>.Deserialize(value);
        }

        public static object Deserialize(this byte[] value, Type type)
        {
            var stream = new MemoryStream(value) { Position = 0 };
            var serializer = serializers.GetOrAdd(type, k => new DataContractSerializer(type));
            return serializer.ReadObject(stream);
        }
    }
}