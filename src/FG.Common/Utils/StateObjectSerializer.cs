using System.IO;
using System.Runtime.Serialization;
using System.Xml;

namespace FG.Common.Utils
{
	public class StateObjectSerializer<T>
	{
		private DataContractSerializer _dataContractSerializer;

		public byte[] Serialize(T state)
		{
			using (var memoryStream = new MemoryStream())
			{
				using (var binaryWriter = XmlDictionaryWriter.CreateBinaryWriter((Stream) memoryStream))
				{
					CreateDataContractSerializer().WriteObject(binaryWriter, (object) state);
					binaryWriter.Flush();
					return memoryStream.ToArray();
				}
			}
		}

		public T Deserialize(byte[] buffer)
		{
			if (buffer == null || buffer.Length == 0)
				return default(T);
			using (var memoryStream = new MemoryStream(buffer))
			{
				using (var binaryReader =
					XmlDictionaryReader.CreateBinaryReader((Stream) memoryStream, XmlDictionaryReaderQuotas.Max))
				{
					return (T) CreateDataContractSerializer().ReadObject(binaryReader);
				}
			}
		}

		private DataContractSerializer CreateDataContractSerializer()
		{
			if (_dataContractSerializer != null) return _dataContractSerializer;
			_dataContractSerializer = new DataContractSerializer(typeof(T), new DataContractSerializerSettings()
			{
				MaxItemsInObjectGraph = int.MaxValue,
			});
			return _dataContractSerializer;
		}
	}
}