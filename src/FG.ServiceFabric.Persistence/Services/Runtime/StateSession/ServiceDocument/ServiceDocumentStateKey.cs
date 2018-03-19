namespace FG.ServiceFabric.Services.Runtime.StateSession.ServiceDocument
{
    public class ServiceDocumentStateKey : ISchemaKey
    {
        public string Schema { get; private set; }
        public string Key { get; private set; }

        public ServiceDocumentStateKey(string schema, string key)
        {
            Schema = schema;
            Key = key;
        }
        internal const string ServiceDocumentStateSchemaName = @"SERVICEINFO";
        
    }
}