namespace FG.ServiceFabric.Actors.Runtime
{
    public class OverloadedStateSessionActorStateProviderSettings
    {
        public bool AlwaysCreateActorDocument { get; set; } = false;
        
        internal OverloadedStateSessionActorStateProviderSettings Clone()
        {
            return new OverloadedStateSessionActorStateProviderSettings
                       {
                           AlwaysCreateActorDocument = this.AlwaysCreateActorDocument,

                       };
        }
    }
}