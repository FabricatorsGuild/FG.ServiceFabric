namespace FG.ServiceFabric.Actors.Runtime.RuntimeInformation
{
    using Microsoft.ServiceFabric.Data;

    public static class ActorRuntimeInformationExtensions
    {

        public static ActorRuntimeInformation SetHasDocumentState(this ActorRuntimeInformation actorRuntimeInformation, bool hasDocumentState)
        {
            actorRuntimeInformation.HasDocumentState = hasDocumentState;
            return actorRuntimeInformation;
        }

        public static ActorRuntimeInformation SetIsSaveInProgress(this ActorRuntimeInformation actorRuntimeInformation, bool isSaveInProgress)
        {
            actorRuntimeInformation.IsSaveInProgress = isSaveInProgress;
            return actorRuntimeInformation;
        }

        public static ActorRuntimeInformation SetIsDeleteInProgress(this ActorRuntimeInformation actorRuntimeInformation, bool isDeleteInProgress)
        {
            actorRuntimeInformation.IsDeleteInProgress = isDeleteInProgress;
            return actorRuntimeInformation;
        }

        public static ActorRuntimeInformation SetDocumentSelfLink(this ActorRuntimeInformation actorRuntimeInformation, string documentSelfLink)
        {
            actorRuntimeInformation.DocumentSelfLink = documentSelfLink;
            return actorRuntimeInformation;
        }

        public static ActorRuntimeInformation SetDocumentSelfLink(this ActorRuntimeInformation actorRuntimeInformation, ConditionalValue<string> documentSelfLink)
        {
            actorRuntimeInformation.DocumentSelfLink = documentSelfLink.HasValue ? documentSelfLink.Value : null;
            return actorRuntimeInformation;
        }
    }
}