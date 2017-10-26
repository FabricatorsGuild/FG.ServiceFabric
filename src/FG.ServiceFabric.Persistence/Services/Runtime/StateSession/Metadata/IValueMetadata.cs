namespace FG.ServiceFabric.Services.Runtime.StateSession.Metadata
{
	public interface IValueMetadata
	{
		string Schema { get; set; }
		string Key { get; set; }
		string Type { get; set; }
		StateWrapper<T> BuildStateWrapper<T>(string id, T value, IServiceMetadata serviceMetadata);

		StateWrapper BuildStateWrapper(string id, IServiceMetadata serviceMetadata);
	}

	public static class ValueMetadataExtensions
	{
		public static void SetType(this IValueMetadata metadata, StateWrapperType type)
		{
			metadata.Type = type.ToString();
		}
	}

	public class ValueMetadata : IValueMetadata
	{
		public ValueMetadata(StateWrapperType type)
		{
			this.SetType(type);
		}

		public ValueMetadata(string schema, string key, string type = null)
		{
			Schema = schema;
			Key = key;
			Type = type;
		}	

		public string Schema { get; set; }
		public string Key { get; set; }
		public string Type { get; set; }
		public virtual StateWrapper<T> BuildStateWrapper<T>(string id, T value, IServiceMetadata serviceMetadata)
		{
			return new StateWrapper<T>(id, value, serviceMetadata, this);
		}
		public StateWrapper BuildStateWrapper(string id, IServiceMetadata serviceMetadata)
		{
			return new StateWrapper(id, serviceMetadata, this);
		}
	}
}