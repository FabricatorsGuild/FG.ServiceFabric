namespace FG.ServiceFabric.Services.Remoting.ExceptionHandler
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.ServiceFabric.Services.Communication.Client;

    /// <summary>
    /// Provides extensions for IExceptionHandlerFactory
    /// </summary>
    public static class ExceptionHandlerFactoryExtensions
    {
        /// <summary>
        /// Gets the exception handlers for all types possible
        /// </summary>
        /// <param name="exceptionHandlerFactory">The exception handler factory</param>
        /// <param name="actorInterfaceType">The actor interface</param>
        /// <param name="additionalTypes">The additional types to get the exception handlers for</param>
        /// <returns>An <see cref="IEnumerable{IExceptionHandler}"/></returns>
        public static IEnumerable<IExceptionHandler> GetExceptionHandlers(this IExceptionHandlerFactory exceptionHandlerFactory, Type actorInterfaceType, params Type[] additionalTypes)
        {
            return GetExceptionHandlersInternal(exceptionHandlerFactory, actorInterfaceType, additionalTypes).Where(exceptionHandler => exceptionHandler != null);
        }

        private static IEnumerable<IExceptionHandler> GetExceptionHandlersInternal(this IExceptionHandlerFactory exceptionHandlerFactory, Type actorInterfaceType, params Type[] additionalTypes)
        {
            yield return exceptionHandlerFactory.GetExceptionHandlerFactory(actorInterfaceType)?.Invoke();

            foreach (var type in additionalTypes)
            {
                yield return exceptionHandlerFactory.GetExceptionHandlerFactory(type)?.Invoke();
            }
        }
    }
}