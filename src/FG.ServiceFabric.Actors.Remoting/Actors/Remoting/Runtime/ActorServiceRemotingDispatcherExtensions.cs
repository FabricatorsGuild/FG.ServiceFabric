using System;
using System.Diagnostics;
using System.Reflection;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Actors.Remoting.Runtime
{
    public static class ActorServiceRemotingDispatcherExtensions
    {
        public static string GetMethodDispatcherMapName(this Microsoft.ServiceFabric.Actors.Remoting.V1.Runtime.ActorServiceRemotingDispatcher that,
            int interfaceId, int methodId)
        {
            try
            {
                var actorServiceFieldInfo = typeof(Microsoft.ServiceFabric.Actors.Remoting.V1.Runtime.ActorServiceRemotingDispatcher).GetField("actorService",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField);
                var actorService = actorServiceFieldInfo?.GetValue(that);
                var methodDispatcherMapPropertyInfo = typeof(ActorService).GetProperty("MethodDispatcherMap",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetProperty);
                var methodDispatcherMap = methodDispatcherMapPropertyInfo?.GetValue(actorService);
                var getDispatcherMethodInfo = methodDispatcherMap?.GetType().GetMethod("GetDispatcher", new Type[] {typeof(int), typeof(int)});
                var methodDispatcher = getDispatcherMethodInfo?.Invoke(methodDispatcherMap,
                    new object[] {interfaceId, methodId});
                var getMethodNameMethodInfo = methodDispatcher?.GetType().GetMethod("GetMethodName",
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                var methodName = getMethodNameMethodInfo?.Invoke(methodDispatcher, new object[] {methodId}) as string;
                return methodName;
            }
            catch (Exception)
            {
                // ignored
            }
            return null;
        }

        public static string GetMethodDispatcherMapName(this Microsoft.ServiceFabric.Services.Remoting.Builder.MethodDispatcherBase that, int interfaceId, int methodId)
        {
            Debug.Assert(that.InterfaceId != interfaceId);
            return that.GetMethodName(methodId);
        }
    }

}