using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FG.ServiceFabric.Services.Remoting.FabricTransport
{
    public static class ServiceRequestContextHelper
    {
        public static Task RunInRequestContext(Action action, IEnumerable<ServiceRequestHeader> headers)
        {
            Task task = null;

            task = new Task(() =>
            {
                Debug.Assert(ServiceRequestContext.Current == null);
                ServiceRequestContext.Current = new ServiceRequestContext(headers);
                try
                {
                    action();
                }
                finally
                {
                    ServiceRequestContext.Current = null;
                }
            });

            task.Start();

            return task;
        }

        public static Task RunInRequestContext(Func<Task> action, IEnumerable<ServiceRequestHeader> headers)
        {
            Task<Task> task = null;

            task = new Task<Task>(async () =>
            {
                Debug.Assert(ServiceRequestContext.Current == null);
                ServiceRequestContext.Current = new ServiceRequestContext(headers);
                try
                {
                    await action();
                }
                finally
                {
                    ServiceRequestContext.Current = null;
                }
            });

            task.Start();

            return task.Unwrap();
        }

        public static Task<TResult> RunInRequestContext<TResult>(Func<Task<TResult>> action, IEnumerable<ServiceRequestHeader> headers)
        {
            Task<Task<TResult>> task = null;

            task = new Task<Task<TResult>>(async () =>
            {
                Debug.Assert(ServiceRequestContext.Current == null);
                ServiceRequestContext.Current = new ServiceRequestContext(headers);
                try
                {
                    return await action();
                }
                finally
                {
                    ServiceRequestContext.Current = null;
                }

            });

            task.Start();

            return task.Unwrap<TResult>();
        }
    }
}