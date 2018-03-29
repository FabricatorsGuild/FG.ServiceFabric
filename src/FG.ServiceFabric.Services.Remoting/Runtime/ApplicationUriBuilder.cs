using System.Fabric;

namespace FG.ServiceFabric.Services.Runtime
{
    public class ApplicationUriBuilder
    {
        public ApplicationUriBuilder()
        {
            ActivationContext = FabricRuntime.GetActivationContext();
        }

        public ApplicationUriBuilder(ICodePackageActivationContext context)
        {
            ActivationContext = context;
        }

        public ApplicationUriBuilder(ICodePackageActivationContext context, string applicationInstance)
        {
            ActivationContext = context;
            ApplicationInstance = applicationInstance;
        }


        /// <summary>
        ///     The name of the application instance that contains he service.
        /// </summary>
        public string ApplicationInstance { get; set; }

        /// <summary>
        ///     The local activation context
        /// </summary>
        public ICodePackageActivationContext ActivationContext { get; set; }

        /// <summary>
        ///     Builds a Uri to the specified Service Instance
        /// </summary>
        /// <param name="serviceInstance"></param>
        /// <returns></returns>
        public ServiceUriBuilder Build(string serviceInstance)
        {
            return new ServiceUriBuilder(ActivationContext, ApplicationInstance,
                serviceInstance);
        }
    }
}