namespace FG.ServiceFabric.Actors.Runtime
{
    public struct ActorRuntimeInformation
    {
        public bool HasDocumentState { get; private set; }

        public bool IsSaveInProgress { get; private set; }

        public bool IsDeleteInProgress { get; private set; }


        public ActorRuntimeInformation Clone()
        {
            return new ActorRuntimeInformation
                       {
                           HasDocumentState = this.HasDocumentState,
                           IsSaveInProgress = this.IsSaveInProgress,
                           IsDeleteInProgress = this.IsDeleteInProgress
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

        public ActorRuntimeInformation SetIsDeleteInProgress(bool isDeleteInProgress)
        {
            var newInformation = this.Clone();
            newInformation.IsDeleteInProgress = isDeleteInProgress;
            return newInformation;
        }

    }
}