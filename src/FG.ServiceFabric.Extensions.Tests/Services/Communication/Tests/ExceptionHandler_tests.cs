// ReSharper disable InconsistentNaming

namespace FG.ServiceFabric.Services.Communication.Tests
{
    using System;

    using FluentAssertions;

    using Microsoft.ServiceFabric.Services.Communication.Client;

    using NUnit.Framework;

    public class ExceptionHandler_tests
    {
        [Test]
        public void ExceptionHandler_of_ExceptionType_should_handle_that_exception()
        {
            var exceptionHandler = new ExceptionHandler<ArgumentException>();

            var exceptionInformation = new ExceptionInformation(new ArgumentException("Some argument error"));
            var operationRetrySettings = new OperationRetrySettings(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(3), 3);
            ExceptionHandlingResult exceptionHandlingResult;

            var canHandle = exceptionHandler.TryHandleException(exceptionInformation, operationRetrySettings, out exceptionHandlingResult);

            canHandle.Should().BeTrue();
            exceptionHandlingResult.Should().BeOfType(typeof(ExceptionHandlingRetryResult));
        }

        [Test]
        public void ExceptionHandler_of_ExceptionType_should_not_handle_other_exceptions()
        {
            var exceptionHandler = new ExceptionHandler<ArgumentException>();

            var exceptionInformation = new ExceptionInformation(new SystemException("A system exception"));
            var operationRetrySettings = new OperationRetrySettings(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(3), 3);
            ExceptionHandlingResult exceptionHandlingResult;

            var canHandle = exceptionHandler.TryHandleException(exceptionInformation, operationRetrySettings, out exceptionHandlingResult);

            canHandle.Should().BeFalse();
            exceptionHandlingResult.Should().BeOfType(typeof(ExceptionHandlingThrowResult));
        }
    }
}