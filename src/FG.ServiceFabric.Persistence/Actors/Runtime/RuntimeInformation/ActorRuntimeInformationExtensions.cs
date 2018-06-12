namespace FG.ServiceFabric.Actors.Runtime.RuntimeInformation
{
    using Microsoft.ServiceFabric.Actors;

    public static class ActorRuntimeInformationCollectionExtensions
    {
        public static ActorRuntimeInformationCollection SetHasDocumentState(this ActorRuntimeInformationCollection collection, ActorId actorId)
        {
            return collection.AddOrUpdateActorRuntimeInformation(actorId, ari => ari.SetHasDocumentState(true));
        }

        public static ActorRuntimeInformationCollection SetIsSaveInProgress(this ActorRuntimeInformationCollection collection, ActorId actorId, bool isSaveInProgress = true)
        {
            return collection.AddOrUpdateActorRuntimeInformation(actorId, ari => ari.SetIsSaveInProgress(isSaveInProgress));
        }

        public static ActorRuntimeInformationCollection IsDeleteInProgress(this ActorRuntimeInformationCollection collection, ActorId actorId, bool isDeleteInProgress = true)
        {
            return collection.AddOrUpdateActorRuntimeInformation(actorId, ari => ari.SetIsDeleteInProgress(isDeleteInProgress));
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
