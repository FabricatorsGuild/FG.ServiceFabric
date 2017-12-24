namespace FG.ServiceFabric.Services.Communication
{
    using System;

    using Microsoft.ServiceFabric.Services.Communication.Client;

    /// <summary>
    /// Provides exception handling for service communication
    ///  </summary>
    /// <typeparam name="TException">The exception type</typeparam>
    public class ExceptionHandler<TException> : IExceptionHandler
        where TException : Exception
    {
        private readonly bool _isTransient;

        private readonly int _maxRetryCount;

        private readonly TimeSpan _retryDelay;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionHandler{TException}"/> class. 
        /// </summary>
        /// <param name="isTransient">
        /// </param>
        public ExceptionHandler(bool isTransient = false)
        {
            this._isTransient = isTransient;
            this._retryDelay = TimeSpan.FromSeconds(1);
            this._maxRetryCount = 5;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionHandler{TException}"/> class. 
        /// </summary>
        /// <param name="isTransient">
        /// </param>
        /// <param name="retryDelay">
        /// </param>
        /// <param name="maxRetryCount">
        /// </param>
        public ExceptionHandler(bool isTransient, TimeSpan retryDelay, int maxRetryCount)
        {
            this._isTransient = isTransient;
            this._retryDelay = retryDelay;
            this._maxRetryCount = maxRetryCount;
        }

        /// <summary>
        /// Tries to handle an exception
        /// </summary>
        /// <param name="exceptionInformation"></param>
        /// <param name="retrySettings"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool TryHandleException(ExceptionInformation exceptionInformation, OperationRetrySettings retrySettings, out ExceptionHandlingResult result)
        {
            if (exceptionInformation.Exception is TException)
            {
                result = new ExceptionHandlingRetryResult(exceptionInformation.Exception, this._isTransient, this._retryDelay, this._maxRetryCount);
                return true;
            }

            result = new ExceptionHandlingThrowResult();
            return false;
        }
    }
}