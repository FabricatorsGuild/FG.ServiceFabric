namespace FG.ServiceFabric.Actors.Runtime
{
    using System;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    public class OverloadedStateSessionActorStateProviderSettings
    {
        public bool AlwaysCreateActorDocument { get; set; } = false;

        public Func<IInnerActorState, Task<string>> StateSerializerFunc { get; set; }

        internal OverloadedStateSessionActorStateProviderSettings Clone()
        {
            return new OverloadedStateSessionActorStateProviderSettings
            {
                AlwaysCreateActorDocument = this.AlwaysCreateActorDocument,
                StateSerializerFunc = this.StateSerializerFunc
            };
        }
    }
}