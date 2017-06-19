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
    }
}