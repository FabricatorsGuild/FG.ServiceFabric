using FG.ServiceFabric.DocumentDb;

namespace FG.ServiceFabric.Actors.Runtime
{
    public partial class DocumentDbActorStateProvider
    {
        public class ConfigurationBuilder
        {
            private readonly DocumentDbActorStateProvider _parent;

            public ConfigurationBuilder(DocumentDbActorStateProvider parent)
            {
                _parent = parent;
            }

            public ConfigurationBuilder Replicate<TStateType>() where TStateType : IPersistedIdentity
            {
                _parent._replicatedTypes.Add(typeof(TStateType), null);
                return this;
            }
        }
    }
}