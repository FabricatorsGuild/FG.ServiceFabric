using System;
using System.Reflection;
using FG.Common.Expressions;
using FG.ServiceFabric.Services.Remoting.FabricTransport;
using Microsoft.ServiceFabric.Services.Communication.Client;

namespace FG.ServiceFabric.Services.Remoting.ExceptionHandler
{
    /// <summary>
    ///     Provides an exception handler factory
    /// </summary>
    public class ExceptionHandlerFactory : IExceptionHandlerFactory
    {
        public static IExceptionHandlerFactory Default { get; } =
            new CachingExceptionHandlerFactoryDecorator(new ExceptionHandlerFactory());

        /// <summary>
        ///     Gets an instance of an exception handler for a specified type
        /// </summary>
        /// <param name="typeToGetExceptionHandlerFor">The type to get the exception handler for</param>
        /// <returns>A <see cref="IExceptionHandler" /> or null if no exception handler is available</returns>
        public Func<IExceptionHandler> GetExceptionHandlerFactory(Type typeToGetExceptionHandlerFor)
        {
            var attribute = typeToGetExceptionHandlerFor.Assembly
                .GetCustomAttribute<FabricTransportRemotingExceptionHandlerAttribute>();
            if (attribute != null && attribute.ExceptionHandlerType != null)
                return CreateExceptionHandlerFactory(attribute.ExceptionHandlerType);

            return null;
        }

        private static Func<IExceptionHandler> CreateExceptionHandlerFactory(Type exceptionHandlerType)
        {
            if (typeof(IExceptionHandler).IsAssignableFrom(exceptionHandlerType))
                return CreateInstanceFactory.CreateInstance(exceptionHandlerType) as Func<IExceptionHandler>;

            return null;
        }
    }
}