using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using FG.Common.Utils;

namespace FG.ServiceFabric.Actors.Runtime.Reminders
{
	public enum ReminderDataSerializationType
	{
		Binary = 0,
		Xml = 1,
		Json = 2
	}

	[Serializable]
	[DataContract]
	public abstract class ReminderDataBase<T>
		where T : ReminderDataBase<T>
	{
		private static readonly DataContractSerializer Serializer = new DataContractSerializer(typeof(T));

		private byte[] SerializeBinary()
		{
			var binaryFormatter = new BinaryFormatter();
			using (var memoryStream = new MemoryStream())
			{
				binaryFormatter.Serialize(memoryStream, this);
				return memoryStream.ToArray();
			}
		}

		private byte[] SerializeXml()
		{
			using (var ms = new MemoryStream())
			{
				var serializer = new DataContractSerializer(typeof(T));
				serializer.WriteObject(ms, this);
				return ms.ToArray();
			}
		}

		public byte[] Serialize(ReminderDataSerializationType type = ReminderDataSerializationType.Binary)
		{
			if (type == ReminderDataSerializationType.Binary)
				return SerializeBinary();

			if (type == ReminderDataSerializationType.Xml)
				return SerializeXml();

			if (type == ReminderDataSerializationType.Json)
				return SerializeJsonUtf8();

			return new byte[0];
		}

		private byte[] SerializeJsonUtf8()
		{
			var serializeObject = Newtonsoft.Json.JsonConvert.SerializeObject(this);
			return System.Text.Encoding.UTF8.GetBytes(serializeObject);
		}

		public static T Deserialize(byte[] data, ReminderDataSerializationType type = ReminderDataSerializationType.Binary)
		{
			if (type == ReminderDataSerializationType.Binary)
				return DeserializeBinary(data);

			if (type == ReminderDataSerializationType.Xml)
				return DeserializeXml(data);

			if (type == ReminderDataSerializationType.Json)
				return DeserializeJsonUtf8(data);

			return default(T);
		}

		private static T DeserializeXml(byte[] data)
		{
			if (data == null)
			{
				return null;
			}
			using (var memStream = new MemoryStream(data))
			{
				try
				{
					var serializer = new DataContractSerializer(typeof(T));
					var readObject = (T) serializer.ReadObject(memStream);
					if (readObject == null)
					{
						throw new ArgumentException($"The byte array is not a serialized state of {typeof(T)}");
					}
					return readObject;
				}
				catch (Exception ex)
				{
					throw new ArgumentException($"The byte array is not a serialized state of {typeof(T)}, {ex.Message}");
				}
			}
		}

		private static T DeserializeBinary(byte[] data)
		{
			using (var memStream = new MemoryStream())
			{
				try
				{
					var binForm = new BinaryFormatter();
					memStream.Write(data, 0, data.Length);
					memStream.Seek(0, SeekOrigin.Begin);
					var readObject = binForm.Deserialize(memStream) as T;
					if (readObject == null)
					{
						throw new ArgumentException($"The byte array is not a serialized state of {typeof(T)}");
					}
					return readObject;
				}
				catch (Exception ex)
				{
					throw new ArgumentException($"The byte array is not a serialized state of {typeof(T)}, {ex.Message}");
				}
			}
		}

		private static T DeserializeJsonUtf8(byte[] data)
		{
			var serializedObject = System.Text.Encoding.UTF8.GetString(data);
			return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(serializedObject);
		}
	}
}