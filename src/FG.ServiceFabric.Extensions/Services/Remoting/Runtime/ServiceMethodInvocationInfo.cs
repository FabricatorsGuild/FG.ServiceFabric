using System;
using System.Fabric;
using System.Runtime.Serialization;
using Microsoft.ServiceFabric.Services.Remoting;

namespace FG.ServiceFabric.Services.Remoting.Runtime
{
    [Serializable]
    [DataContract]
    public class ServiceMethodInvocationInfo
    {
        public ServiceMethodInvocationInfo() { }

        public ServiceMethodInvocationInfo(
            ServiceContext serviceContext,
            ServiceRemotingMessageHeaders messageHeaders,
            string methodName,
            string invocationId,
            DateTime start,
            TimeSpan duration = default(TimeSpan),
            bool isSuccess = true
        )
        {
            this.ServiceName = serviceContext.ServiceName;
            this.ServiceInterfaceId = messageHeaders.InterfaceId;
            this.ReplicaOrInstanceId = serviceContext.ReplicaOrInstanceId;
            this.PartitionId = serviceContext.PartitionId;
            this.NodeName = serviceContext.NodeContext.NodeName;
            this.Method = methodName;
            this.InvocationId = invocationId;
            this.MethodId = messageHeaders.MethodId;
            this.InvocationId = messageHeaders.InvocationId;
            this.Start = start;
            this.Duration = duration;
            this.IsSuccess = isSuccess;
        }

        [DataMember]
        public Uri  ServiceName { get; set; }
        [DataMember]
        public int ServiceInterfaceId { get; set; }
        [DataMember]
        public long ReplicaOrInstanceId { get; set; }
        [DataMember]
        public Guid PartitionId { get; set; }
        [DataMember]
        public string NodeName { get; set; }
        [DataMember]
        public string Method { get; set; }
        [DataMember]
        public int MethodId { get; set; }
        [DataMember]
        public string InvocationId { get; set; }
        [DataMember]
        public DateTime Start { get; set; }
        [DataMember]
        public TimeSpan Duration { get; set; }
        [DataMember]
        public bool IsSuccess { get; set; }
    }
}