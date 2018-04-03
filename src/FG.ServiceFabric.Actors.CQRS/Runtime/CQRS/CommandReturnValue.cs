using System.Runtime.Serialization;

// ReSharper disable once CheckNamespace - Set the namespace to the old namespace used in 1.3 and older of FG.ServiceFabric.CQRS
namespace FG.ServiceFabric.CQRS
{
    [DataContract]
    internal sealed class CommandReturnValue
    {
        private CommandReturnValue()
        {
        }

        private CommandReturnValue(object returnValue)
        {
            ReturnValue = returnValue;
        }

        [DataMember]
        public object ReturnValue { get; set; }

        [IgnoreDataMember]
        public bool HasReturnValue => ReturnValue != null;

        public static CommandReturnValue None()
        {
            return new CommandReturnValue();
        }

        public static CommandReturnValue Create(object returnValue)
        {
            return new CommandReturnValue(returnValue);
        }
    }
}