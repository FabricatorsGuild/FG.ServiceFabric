namespace FG.ServiceFabric.Actors.Runtime
{
    public struct ActorRuntimeInformation
    {
        public bool HasDocumentState { get; private set; }

        public bool IsSaveInProgress { get; private set; }

        public ActorRuntimeInformation Clone()
        {
            return new ActorRuntimeInformation
                       {
                           HasDocumentState = this.HasDocumentState,
                           IsSaveInProgress = this.IsSaveInProgress
                       };
        }


        public ActorRuntimeInformation SetHasDocumentState(bool hasDocumentState)
        {
            var newInformation = this.Clone();
            newInformation.HasDocumentState = hasDocumentState;
            return newInformation;
        }

        public ActorRuntimeInformation SetIsSaveInProgress(bool isSaveInProgress)
        {
            var newInformation = this.Clone();
            newInformation.IsSaveInProgress = isSaveInProgress;
            return newInformation;
        }

    }
}