namespace FG.ServiceFabric.Actors.Runtime
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class DocumentStateDefinition
    {
        private readonly Dictionary<string, Type> stateTypes;

        public DocumentStateDefinition()
        {
            this.stateTypes = new Dictionary<string, Type>();
        }

        public DocumentStateDefinition(IEnumerable<KeyValuePair<string, Type>> stateTypes)
        {
            this.stateTypes = stateTypes.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
    }
}