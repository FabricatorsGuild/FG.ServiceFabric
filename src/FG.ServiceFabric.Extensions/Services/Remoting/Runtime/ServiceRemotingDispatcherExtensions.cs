using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using FG.ServiceFabric.Services.Remoting.FabricTransport;
using FG.ServiceFabric.Services.Remoting.FabricTransport.Client;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;

namespace FG.ServiceFabric.Services.Remoting.Runtime
{
    public static class ServiceRemotingDispatcherExtensions
    {
        public static string GetMethodDispatcherMapName(this Microsoft.ServiceFabric.Services.Remoting.Runtime.ServiceRemotingDispatcher that, int interfaceId, int methodId)
        {
            try
            {
                var methodDispatcherMapFieldInfo = typeof(Microsoft.ServiceFabric.Services.Remoting.Runtime.ServiceRemotingDispatcher).GetField("methodDispatcherMap",
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField);
                var methodDispatcherMap = methodDispatcherMapFieldInfo?.GetValue(that);
                var methodDispatcher = methodDispatcherMap?.GetType()
                    .InvokeMember("Item", BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty, null, methodDispatcherMap,
                        new object[] {interfaceId});
                var getMethodNameMethodInfo = methodDispatcher?.GetType()
                    .GetMethod("Microsoft.ServiceFabric.Services.Remoting.IMethodDispatcher.GetMethodName", BindingFlags.NonPublic | BindingFlags.Instance);
                var methodName = getMethodNameMethodInfo?.Invoke(methodDispatcher, new object[] {methodId}) as string;
                return methodName;
            }
            catch (Exception)
            {
                // Ignore
                return null;
            }
        }

        public static Task RunInRequestContext(this IServiceRemotingMessageHandler serviceRemotingDispatcher, Action action, IEnumerable<ServiceRequestHeader> headers)
        {
            return ServiceRequestContextHelper.RunInRequestContext(action, headers);
        }

        public static Task RunInRequestContext(this IServiceRemotingMessageHandler serviceRemotingDispatcher, Func<Task> action, IEnumerable<ServiceRequestHeader> headers)
        {
            return ServiceRequestContextHelper.RunInRequestContext(action, headers);
        }

        public static Task<TResult> RunInRequestContext<TResult>(this IServiceRemotingMessageHandler serviceRemotingDispatcher, Func<Task<TResult>> action, IEnumerable<ServiceRequestHeader> headers)
        {
            return ServiceRequestContextHelper.RunInRequestContext(action, headers);
        }
    }
}