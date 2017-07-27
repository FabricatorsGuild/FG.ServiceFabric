using System;
using FG.ServiceFabric.Utils;

namespace FG.ServiceFabric.Testing.Mocks
{
    public class ForTestsSettingsProvider : ISettingsProvider
    {
        public string this[string key]
        {
            get { throw new NotImplementedException(); }
        }

        public static ISettingsProvider Create()
        {
            return new ForTestsSettingsProvider();
        }
    }
}
