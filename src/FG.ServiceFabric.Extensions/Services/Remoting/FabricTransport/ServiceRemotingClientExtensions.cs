using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FG.ServiceFabric.Services.Remoting.FabricTransport.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;

namespace FG.ServiceFabric.Services.Remoting.FabricTransport
{
    public static class ServiceRemotingClientExtensions
    {
        public static Task RunInRequestContext(this IServiceRemotingClient serviceRemotingClient, Action action, IEnumerable<ServiceRequestHeader> headers)
        {
            return ServiceRequestContextHelper.RunInRequestContext(action, headers);
        }

        public static Task RunInRequestContext(this IServiceRemotingClient serviceRemotingClient, Func<Task> action, IEnumerable<ServiceRequestHeader> headers)
        {
            return ServiceRequestContextHelper.RunInRequestContext(action, headers);
        }

        public static Task<TResult> RunInRequestContext<TResult>(this IServiceRemotingClient serviceRemotingClient, Func<Task<TResult>> action, IEnumerable<ServiceRequestHeader> headers)
        {
            return ServiceRequestContextHelper.RunInRequestContext(action, headers);
        }
    }
}