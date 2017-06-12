//using System.Fabric;
//using System.Threading;
//using System.Threading.Tasks;
//using Microsoft.ServiceFabric.Actors.Client;
//using Microsoft.ServiceFabric.Services.Remoting.Client;

//namespace CodeEffect.ServiceFabric.Services.Runtime
//{
//    public abstract class StatelessService : Microsoft.ServiceFabric.Services.Runtime.StatelessService
//    {
//        private IServiceProxyFactory _serviceProxyFactory;
//        private ApplicationUriBuilder _applicationUriBuilder;
//        private CancellationTokenSource _tokenSource = null;
//        private IActorProxyFactory _actorProxyFactory;

//        public StatelessService(StatelessServiceContext serviceContext) : base(serviceContext)
//        {
//            _applicationUriBuilder = new ApplicationUriBuilder(this.Context.CodePackageActivationContext);
//        }

//        public ApplicationUriBuilder ApplicationUriBuilder => _applicationUriBuilder ?? (_applicationUriBuilder = new ApplicationUriBuilder());

//        public IActorProxyFactory ActorProxyFactory => _actorProxyFactory ?? (_actorProxyFactory = new ActorProxyFactory());

//        public IServiceProxyFactory ServiceProxyFactory => _serviceProxyFactory ?? (_serviceProxyFactory = new ServiceProxyFactory());        
//    }
//}