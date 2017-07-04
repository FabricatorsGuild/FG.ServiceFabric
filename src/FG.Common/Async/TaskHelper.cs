using System;
using System.Threading.Tasks;

namespace FG.Common.Async
{
    public static class TaskHelper
    {
        public static TResult Await<TResult>(this Task<TResult> task)
        {
            task.Wait();
            return task.Result;
        }

        public static void FireAndForget(this Task task)
        {
            Task.Run(async () => await task).ConfigureAwait(false);
        }

        public static int FireAndHandleLater(this Task task,
            Action<Task> onDone = null,
            Action<Task, AggregateException> onFault = null,
            Action<Task> onCancel = null,
            Action<Task> onOther = null)
        {
            Task.Run(async () => await task).ContinueWith(executedTask =>
            {
                if (executedTask.IsCompleted)
                {
                    if (executedTask.IsCompleted && !executedTask.IsFaulted && !executedTask.IsCanceled)
                    {
                        onDone?.Invoke(task);
                        Console.WriteLine("Finished task, yay!");
                    }

                    if (executedTask.IsCompleted && executedTask.IsFaulted)
                    {
                        onFault?.Invoke(task, executedTask.Exception);
                        Console.WriteLine("Failed task, oh noes.");
                    }

                    if (executedTask.IsCompleted && executedTask.IsCanceled)
                    {
                        onCancel?.Invoke(task);
                        Console.WriteLine("Someone pushed stop...");
                    }
                }
                else
                {
                    onOther?.Invoke(task);
                }
            }).ConfigureAwait(false);
            return task.Id;
        }

        public static int FireAndHandleLater(
            this Task task,
            Func<Task, Task> onDone = null,
            Func<Task, AggregateException, Task> onFault = null,
            Func<Task, Task> onCancel = null,
            Func<Task, Task> onOther = null)
        {
            Task.Run(async () => await task).ContinueWith(async executedTask =>
            {
                if (executedTask.IsCompleted)
                {
                    if (executedTask.IsCompleted && !executedTask.IsFaulted && !executedTask.IsCanceled)
                    {
                        if (onDone != null)
                        {
                            await onDone(task);
                        }
                    }

                    if (executedTask.IsCompleted && executedTask.IsFaulted)
                    {
                        if (onFault != null)
                        {
                            await onFault(task, executedTask.Exception);
                        }
                    }

                    if (executedTask.IsCompleted && executedTask.IsCanceled)
                    {
                        if (onCancel != null)
                        {
                            await onCancel(task);
                        }
                    }
                }
                else
                {
                    if (onOther != null)
                    {
                        await onOther(task);
                    }
                }
            }).ConfigureAwait(false);
            return task.Id;
        }

        public static int FireAndHandleLater<TResult>(this Task<TResult> task,
            Action<Task, TResult> onDone = null,
            Action<Task, AggregateException> onFault = null,
            Action<Task> onCancel = null,
            Action<Task> onOther = null)
        {
            Task.Run(async () => await task).ContinueWith(executedTask =>
            {
                if (executedTask.IsCompleted)
                {
                    if (executedTask.IsCompleted && !executedTask.IsFaulted && !executedTask.IsCanceled)
                    {
                        onDone?.Invoke(task, executedTask.Result);
                        Console.WriteLine("Finished task, yay!");
                    }

                    if (executedTask.IsCompleted && executedTask.IsFaulted)
                    {
                        onFault?.Invoke(task, executedTask.Exception);
                        Console.WriteLine("Failed task, oh noes.");
                    }

                    if (executedTask.IsCompleted && executedTask.IsCanceled)
                    {
                        onCancel?.Invoke(task);
                        Console.WriteLine("Someone pushed stop...");
                    }
                }
                else
                {
                    onOther?.Invoke(task);
                }
            }).ConfigureAwait(false);
            return task.Id;
        }

        public static int FireAndHandleLater<TResult>(
            this Task<TResult> task,
            Func<Task, TResult, Task> onDone = null,
            Func<Task, AggregateException, Task> onFault = null,
            Func<Task, Task> onCancel = null,
            Func<Task, Task> onOther = null)
        {
            Task.Run(async () => await task).ContinueWith(async executedTask =>
            {
                if (executedTask.IsCompleted)
                {
                    if (executedTask.IsCompleted && !executedTask.IsFaulted && !executedTask.IsCanceled)
                    {
                        if (onDone != null)
                        {
                            await onDone(task, executedTask.Result);
                        }
                    }

                    if (executedTask.IsCompleted && executedTask.IsFaulted)
                    {
                        if (onFault != null)
                        {
                            await onFault(task, executedTask.Exception);
                        }
                    }

                    if (executedTask.IsCompleted && executedTask.IsCanceled)
                    {
                        if (onCancel != null)
                        {
                            await onCancel(task);
                        }
                    }
                }
                else
                {
                    if (onOther != null)
                    {
                        await onOther(task);
                    }
                }
            }).ConfigureAwait(false);
            return task.Id;
        }

    }
}