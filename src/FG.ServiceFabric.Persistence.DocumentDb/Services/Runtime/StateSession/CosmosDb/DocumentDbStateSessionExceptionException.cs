using System;
using System.Collections.Specialized;
using System.Net;
using System.Runtime.Serialization;
using Microsoft.Azure.Documents;

namespace FG.ServiceFabric.Services.Runtime.StateSession.CosmosDb
{
    [Serializable]
    public class DocumentDbStateSessionExceptionException : Exception
    {
        public DocumentDbStateSessionExceptionException()
        {
        }

        public DocumentDbStateSessionExceptionException(string message) : base(message)
        {
        }

        public DocumentDbStateSessionExceptionException(string message, DocumentClientException documentClientException)
            : base(message)
        {
            DocumentClientError = documentClientException.Error;
            DocumentClientActivityId = documentClientException.ActivityId;
            DocumentClientRetryAfter = documentClientException.RetryAfter;
            DocumentClientResponseHeaders = documentClientException.ResponseHeaders;
            DocumentClientStatusCode = documentClientException.StatusCode;
            DocumentClientRequestCharge = documentClientException.RequestCharge;
            DocumentClientMessage = documentClientException.Message;
        }

        protected DocumentDbStateSessionExceptionException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }


        //
        // Summary:
        //     Gets the error code associated with the exception in the Azure DocumentDB database
        //     service.
        public Error DocumentClientError { get; }

        //
        // Summary:
        //     Gets the activity ID associated with the request from the Azure DocumentDB database
        //     service.
        public string DocumentClientActivityId { get; }

        //
        // Summary:
        //     Gets the recommended time interval after which the client can retry failed requests
        //     from the Azure DocumentDB database service
        public TimeSpan DocumentClientRetryAfter { get; }

        //
        // Summary:
        //     Gets the headers associated with the response from the Azure DocumentDB database
        //     service.
        public NameValueCollection DocumentClientResponseHeaders { get; }

        //
        // Summary:
        //     Gets or sets the request status code in the Azure DocumentDB database service.
        public HttpStatusCode? DocumentClientStatusCode { get; }

        //
        // Summary:
        //     Cost of the request in the Azure DocumentDB database service.
        public double DocumentClientRequestCharge { get; }

        //
        // Summary:
        //     Gets a message that describes the current exception from the Azure DocumentDB
        //     database service.
        public string DocumentClientMessage { get; }
    }
}