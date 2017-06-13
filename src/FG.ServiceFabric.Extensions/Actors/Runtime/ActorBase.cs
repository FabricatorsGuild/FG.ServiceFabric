using System.Threading.Tasks;
using FG.ServiceFabric.Services.Runtime;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;

namespace FG.ServiceFabric.Actors.Runtime
{
    public abstract partial class ActorBase : Microsoft.ServiceFabric.Actors.Runtime.Actor
    {
        private IServiceProxyFactory _serviceProxyFactory;
        private IActorProxyFactory _actorProxyFactory;
        private ApplicationUriBuilder _applicationUriBuilder;

        protected ActorBase(Microsoft.ServiceFabric.Actors.Runtime.ActorService actorService, ActorId actorId) : base(actorService, actorId)
        {
            _applicationUriBuilder = new ApplicationUriBuilder(actorService.Context.CodePackageActivationContext);
            
        }

        public ApplicationUriBuilder ApplicationUriBuilder => _applicationUriBuilder ?? (_applicationUriBuilder = new ApplicationUriBuilder());

        public IActorProxyFactory ActorProxyFactory => _actorProxyFactory ?? (_actorProxyFactory = new ActorProxyFactory());

        public IServiceProxyFactory ServiceProxyFactory => _serviceProxyFactory ?? (_serviceProxyFactory = new ServiceProxyFactory());

        /// <summary>
        /// Adding this method to support DI/Testing 
        /// We need to do some work to create the actor object and make sure it is constructed completely
        /// In local testing we can inject the components we need, but in a real cluster
        /// those items are not established until the actor object is activated. Thus we need to 
        /// have this method so that the tests can have the same init path as the actor would in prod
        /// </summary>
        /// <returns></returns>
        //public async Task InternalActivate(
        //    IServiceProxyFactory proxyFactory,
        //    IActorProxyFactory actorProxyFactory)
        //{
        //    this._serviceProxyFactory = proxyFactory;
        //    this._actorProxyFactory = actorProxyFactory;

        //    await this.OnActivateAsync();
        //}
    }
}