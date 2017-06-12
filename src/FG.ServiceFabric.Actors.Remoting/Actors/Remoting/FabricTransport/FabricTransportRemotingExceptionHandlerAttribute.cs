using System;

namespace FG.ServiceFabric.Actors.Remoting.FabricTransport
{
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    sealed class FabricTransportRemotingExceptionHandlerAttribute : Attribute
    {
        public Type ExceptionHandlerType { get; }

        public FabricTransportRemotingExceptionHandlerAttribute(Type exceptionHandlerType)
        {
            ExceptionHandlerType = exceptionHandlerType;
        }
    }
}