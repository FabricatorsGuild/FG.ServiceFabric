namespace FG.ServiceFabric.Testing.Mocks.Services.Remoting.Client
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using FG.Common.Utils;

    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Client;
    using Microsoft.ServiceFabric.Actors.Runtime;
    using Microsoft.ServiceFabric.Services.Remoting;
    using Microsoft.ServiceFabric.Services.Remoting.Client;

    using Serpent.InterfaceProxy.Extensions;
    using Serpent.InterfaceProxy.Implementations.ProxyTypeBuilder;

    using ActorBase = FG.ServiceFabric.Actors.Runtime.ActorBase;

    public class BaseServiceProxy<TInterface> : BaseMethodProxyWithMethodNames<TInterface, IServiceProxy>
        where TInterface : IService
    {
        static BaseServiceProxy()
        {

        }

        public BaseServiceProxy(TInterface actorInterface, IServiceProxy serviceProxy)
            : base(actorInterface, serviceProxy)
        {
        }

        protected override async Task ExecuteAsync(string methodName, Func<TInterface, Task> func)
        {
            try
            {
                await func(this.InnerType1Reference);
            }
            finally
            {
            }
        }

        protected override async Task<TResult> ExecuteAsync<TResult>(string methodName, Func<TInterface, Task<TResult>> func)
        {
            try
            {
                return await func(this.InnerType1Reference);
            }
            finally
            {
            }
        }

        protected override async Task ExecuteAsync<TParameter>(string methodName, TParameter parameter, Func<TParameter, TInterface, Task> func)
        {
            try
            {
                await func(parameter, this.InnerType1Reference);
            }
            finally
            {
            }
        }

        protected override async Task<TResult> ExecuteAsync<TParameter, TResult>(string methodName, TParameter parameter, Func<TParameter, TInterface, Task<TResult>> func)
        {
            try
            {
                return await func(parameter, this.InnerType1Reference);
            }
            finally
            {
            }
        }
    }
}