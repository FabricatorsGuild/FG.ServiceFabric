using System;
using System.Collections.Concurrent;
using Microsoft.ServiceFabric.Services.Communication.Client;

namespace FG.ServiceFabric.Services.Remoting.ExceptionHandler
{
    /// <summary>
    ///     Provides a decorator that caches exception handlers for types
    /// </summary>
    public class CachingExceptionHandlerFactoryDecorator : IExceptionHandlerFactory
    {
        private readonly ConcurrentDictionary<Type, Func<IExceptionHandler>> exceptionHandlerFactories =
            new ConcurrentDictionary<Type, Func<IExceptionHandler>>();

        private readonly IExceptionHandlerFactory innerExceptionHandlerFactory;

        /// <summary>
        ///     Initializes a new instance of the <see cref="CachingExceptionHandlerFactoryDecorator" /> class.
        /// </summary>
        /// <param name="innerExceptionHandlerFactory">
        ///     The exception handler factory used when a type is not already cached
        /// </param>
        public CachingExceptionHandlerFactoryDecorator(IExceptionHandlerFactory innerExceptionHandlerFactory)
        {
            this.innerExceptionHandlerFactory = innerExceptionHandlerFactory ??
                                                throw new ArgumentNullException(nameof(innerExceptionHandlerFactory));
        }

        /// <summary>
        ///     Gets an instance of an exception handler for a specified type
        /// </summary>
        /// <param name="typeToGetExceptionHandlerFor">The type to get the exception handler for</param>
        /// <returns>A <see cref="IExceptionHandler" /> or null if no exception handler is available</returns>
        public Func<IExceptionHandler> GetExceptionHandlerFactory(Type typeToGetExceptionHandlerFor)
        {
            return exceptionHandlerFactories.GetOrAdd(typeToGetExceptionHandlerFor,
                innerExceptionHandlerFactory.GetExceptionHandlerFactory);
        }
    }
}