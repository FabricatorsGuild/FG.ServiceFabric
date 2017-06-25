using System;
using System.IO;
using Newtonsoft.Json;

namespace FG.ServiceFabric.Data
{
    public partial class EventStore<TEvent>
    {
        public class SomeBus
        {
            private readonly JsonSerializerSettings _settings;
            private const string BaseFolderPath = @"C:\Temp\";

            public SomeBus()
            {
                _settings = new JsonSerializerSettings()
                {
                    TypeNameHandling = TypeNameHandling.All
                };
            }
            private static string GetFolderPath()
            {
                var folder = Path.Combine(BaseFolderPath, "Bus");
                Directory.CreateDirectory(folder);
                return folder;
            }

            public void Publish(object message)
            {
                try
                {
                    var addData = JsonConvert.SerializeObject(message, Formatting.Indented, _settings);
                    File.WriteAllText(Path.Combine(GetFolderPath(), Guid.NewGuid() + ".json"), addData);

                }
                catch (Exception e)
                {
                    throw;
                }
            }
        }
    }
}