namespace FG.ServiceFabric.Actors.Runtime.RuntimeInformation
{
    public struct ActorRuntimeInformation
    {
        public bool HasDocumentState { get; set; }

        public bool IsSaveInProgress { get; set; }

        public bool IsDeleteInProgress { get; set; }

        public string DocumentSelfLink { get; set; }

        public ActorRuntimeInformation Clone()
        {
            return new ActorRuntimeInformation
                       {
                           HasDocumentState = this.HasDocumentState,
                           IsSaveInProgress = this.IsSaveInProgress,
                           IsDeleteInProgress = this.IsDeleteInProgress,
                           DocumentSelfLink = this.DocumentSelfLink
                       };
        }
    }
}