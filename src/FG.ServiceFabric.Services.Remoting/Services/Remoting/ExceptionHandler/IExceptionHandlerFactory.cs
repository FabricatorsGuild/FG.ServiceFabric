using System;
using Microsoft.ServiceFabric.Services.Communication.Client;

namespace FG.ServiceFabric.Services.Remoting.ExceptionHandler
{
    /// <summary>
    ///     Provides exception handlers for the specified types
    /// </summary>
    public interface IExceptionHandlerFactory
    {
        /// <summary>
        ///     Gets an instance of an exception handler for a specified type
        /// </summary>
        /// <param name="typeToGetExceptionHandlerFor">The type to get the exception handler for</param>
        /// <returns>A <see cref="IExceptionHandler" /> or null if no exception handler is available</returns>
        Func<IExceptionHandler> GetExceptionHandlerFactory(Type typeToGetExceptionHandlerFor);
    }
}