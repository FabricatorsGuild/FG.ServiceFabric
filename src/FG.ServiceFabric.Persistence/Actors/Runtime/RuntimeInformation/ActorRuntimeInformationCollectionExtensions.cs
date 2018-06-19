namespace FG.ServiceFabric.Actors.Runtime.RuntimeInformation
{
    using Microsoft.ServiceFabric.Actors;

    public static class ActorRuntimeInformationCollectionExtensions
    {
        public static ActorRuntimeInformationCollection SetHasDocumentState(this ActorRuntimeInformationCollection collection, ActorId actorId)
        {
            return collection.Set(actorId, ari => ari.SetHasDocumentState(true));
        }

        public static ActorRuntimeInformationCollection SetIsSaveInProgress(this ActorRuntimeInformationCollection collection, ActorId actorId, bool isSaveInProgress = true)
        {
            return collection.Set(actorId, ari => ari.SetIsSaveInProgress(isSaveInProgress));
        }

        public static ActorRuntimeInformationCollection IsDeleteInProgress(this ActorRuntimeInformationCollection collection, ActorId actorId, bool isDeleteInProgress = true)
        {
            return collection.Set(actorId, ari => ari.SetIsDeleteInProgress(isDeleteInProgress));
        }

        public static ActorRuntimeInformationCollection SetSelfDocumentLink(this ActorRuntimeInformationCollection collection, ActorId actorId, string selfDocumentLink)
        {
            return collection.Set(actorId, ari => ari.SetDocumentSelfLink(selfDocumentLink));
        }

        public static bool IsSaveInProgress(this ActorRuntimeInformationCollection collection, ActorId actorId)
        {
            var value = collection.TryGetActorRuntimeInformation(actorId);
            if (value.HasValue == false)
            {
                return false;
            }

            return value.Value.IsSaveInProgress;
        }
    }
}
