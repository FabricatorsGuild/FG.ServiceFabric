//using System.Collections.Generic;
//using System.Fabric;
//using System.Threading;
//using System.Threading.Tasks;
//using Microsoft.ServiceFabric.Actors.Client;
//using Microsoft.ServiceFabric.Data;
//using Microsoft.ServiceFabric.Services.Communication.Runtime;
//using Microsoft.ServiceFabric.Services.Remoting;
//using Microsoft.ServiceFabric.Services.Remoting.Client;
//using Microsoft.ServiceFabric.Services.Remoting.Runtime;

//namespace CodeEffect.ServiceFabric.Services.Runtime
//{
//    public abstract class StatefulService : Microsoft.ServiceFabric.Services.Runtime.StatefulService, IService
//    {
//        private IServiceProxyFactory _serviceProxyFactory;
//        private ApplicationUriBuilder _applicationUriBuilder;
//        private CancellationTokenSource _tokenSource = null;
//        private IActorProxyFactory _actorProxyFactory;

//        public StatefulService(StatefulServiceContext serviceContext) : base(serviceContext)
//        {
//            _applicationUriBuilder = new ApplicationUriBuilder(this.Context.CodePackageActivationContext);
//        }

//        public StatefulService(StatefulServiceContext serviceContext, IReliableStateManagerReplica reliableStateManagerReplica) : base(serviceContext, reliableStateManagerReplica)
//        {
//            _applicationUriBuilder = new ApplicationUriBuilder(this.Context.CodePackageActivationContext);
//        }

//        public ApplicationUriBuilder ApplicationUriBuilder => _applicationUriBuilder ?? (_applicationUriBuilder = new ApplicationUriBuilder());

//        public IActorProxyFactory ActorProxyFactory => _actorProxyFactory ?? (_actorProxyFactory = new ActorProxyFactory());

//        public IServiceProxyFactory ServiceProxyFactory => _serviceProxyFactory ?? (_serviceProxyFactory = new ServiceProxyFactory());                
//    }
//}