using System;
using System.IO;
using System.Runtime.Serialization;

namespace FG.Common.Utils
{
    public static class SerializationUtil
    {
        public static byte[] Serialize<T>(this T value)
        {
            var stream = new MemoryStream();
            var serializer = new DataContractSerializer(typeof(T));
            serializer.WriteObject(stream, value);

            return stream.ReadToEnd();
        }

        public static T Deserialize<T>(this byte[] value)
        {
            var stream = new MemoryStream(value) {Position = 0};
            var serializer = new DataContractSerializer(typeof(T));
            return (T) serializer.ReadObject(stream);
        }

        public static object Deserialize(this byte[] value, Type type)
        {
            var stream = new MemoryStream(value) {Position = 0};
            var serializer = new DataContractSerializer(type);
            return serializer.ReadObject(stream);
        }

        private static byte[] ReadToEnd(this Stream input)
        {
            input.Position = 0;
            var buffer = new byte[16 * 1024];
            using (var ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                    ms.Write(buffer, 0, read);
                return ms.ToArray();
            }
        }
    }
}