using System;
using Microsoft.ServiceFabric.Services.Communication.Client;

namespace FG.ServiceFabric.Services.Communication
{
    public class ExceptionHandler<TException> : IExceptionHandler
        where TException : Exception
    {
        private readonly bool _isTransient;
        private readonly TimeSpan _retryDelay;
        private readonly int _maxRetryCount;

        public ExceptionHandler(bool isTransient = false)
        {
            _isTransient = isTransient;
            _retryDelay = TimeSpan.FromSeconds(1);
            _maxRetryCount = 5;
        }

        public ExceptionHandler(bool isTransient, TimeSpan retryDelay, int maxRetryCount)
        {
            _isTransient = isTransient;
            _retryDelay = retryDelay;
            _maxRetryCount = maxRetryCount;
        }

        public bool TryHandleException(
            ExceptionInformation exceptionInformation,
            OperationRetrySettings retrySettings,
            out ExceptionHandlingResult result)
        {
            if (exceptionInformation.Exception is TException)
            {
                result = new ExceptionHandlingRetryResult(
                    exception: exceptionInformation.Exception,
                    isTransient: _isTransient,
                    retryDelay: _retryDelay,
                    maxRetryCount: _maxRetryCount);
                return true;
            }
            result = new ExceptionHandlingThrowResult();
            return false;
        }
    }
}
