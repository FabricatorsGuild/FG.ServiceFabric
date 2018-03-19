using System;
using System.Reflection;
using Microsoft.ServiceFabric.Services.Remoting.Builder;

namespace FG.ServiceFabric.Services.Remoting.Runtime
{
    public static class ServiceRemotingDispatcherExtensions
    {
        public static string GetMethodDispatcherMapName(
            this Microsoft.ServiceFabric.Services.Remoting.V1.Runtime.ServiceRemotingDispatcher that, int interfaceId,
            int methodId)
        {
            try
            {
                var methodDispatcherMapFieldInfo =
                    typeof(Microsoft.ServiceFabric.Services.Remoting.V1.Runtime.ServiceRemotingDispatcher).GetField(
                        "methodDispatcherMap",
                        BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField);
                var methodDispatcherMap = methodDispatcherMapFieldInfo?.GetValue(that);
                var methodDispatcher = methodDispatcherMap?.GetType()
                    .InvokeMember("Item", BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty, null,
                        methodDispatcherMap,
                        new object[] {interfaceId});
                var getMethodNameMethodInfo =
                    methodDispatcher?.GetType()
                        .GetInterface("Microsoft.ServiceFabric.Services.Remoting.IMethodDispatcher")
                        .GetMethod("GetMethodName");
                var methodName = getMethodNameMethodInfo?.Invoke(methodDispatcher, new object[] {methodId}) as string;
                return methodName;
            }
            catch (Exception)
            {
                // Ignore
                return null;
            }
        }

        public static string GetMethodDispatcherMapName(
            this MethodDispatcherBase that, int interfaceId, int methodId)
        {
            if (that.InterfaceId == interfaceId)
                return that.GetMethodName(methodId);
            return null;
        }
    }
}