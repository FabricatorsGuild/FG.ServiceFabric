using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;

namespace FG.ServiceFabric.Tests.Actor.Interfaces
{
    /// <summary>
    /// This interface defines the methods exposed by an actor.
    /// Clients use this interface to interact with the actor that implements it.
    /// </summary>
    public interface IComplexActor : IActor
    {
        Task<ComplexType> GetComplexTypeAsync();

        Task SetComplexTypeAsync(string value);
    }


    [DataContract]
    public class ComplexType
    {
        [DataMember]
        public Guid SomeId { get; set; }
        [DataMember]
        public List<string> ListOfStrings { get; set; }
        [DataMember]
        public List<InnerComplexType> ListOfSomething { get; set; }
    }

    [DataContract]
    [KnownType(typeof(SomeImpl))]
    public class InnerComplexType
    {
        [DataMember]
        public Guid SomeId { get; set; }
        [DataMember]
        public ISomeInterface[] ArrayOfInterfaces { get; set; }
    }

    public interface ISomeInterface
    {
        string Value { get; set; }
    }

    [DataContract]
    public class SomeImpl : ISomeInterface
    {
        [DataMember]
        public string Value { get; set; }
    }
}