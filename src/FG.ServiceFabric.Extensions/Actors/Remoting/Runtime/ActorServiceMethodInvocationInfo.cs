using System;
using System.Fabric;
using System.Runtime.Serialization;
using FG.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Services.Remoting;

namespace FG.ServiceFabric.Actors.Remoting.Runtime
{
    [Serializable]
    [DataContract]
    public class ActorServiceMethodInvocationInfo : ServiceMethodInvocationInfo
    {
        public ActorServiceMethodInvocationInfo() { }

        public ActorServiceMethodInvocationInfo(
            ServiceContext serviceContext,
            ServiceRemotingMessageHeaders messageHeaders,
            string methodName,
            string invocationId,
            ActorId actorId,
            string actorService,
            DateTime start,
            TimeSpan duration = default(TimeSpan),
            bool isSuccess = true
        ) : base(serviceContext, messageHeaders, methodName, invocationId, start, duration, isSuccess)
        {
            ActorId = actorId;
            ActorService = actorService;
        }

        [DataMember]
        public ActorId ActorId { get; set; }
        [DataMember]
        public string ActorService { get; set; }
    }
}