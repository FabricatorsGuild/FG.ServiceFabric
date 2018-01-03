using System;
using System.Threading.Tasks;

namespace FG.Common.Async
{
    public static class TaskHelper
    {
        // EB: Perhaps naming this ContinueWith is more appropriate, since it's a conditional ContinueWith
        public static int FireAndHandleLater(this Task task,
            Action<Task> onDone = null,
            Action<Task, AggregateException> onFault = null,
            Action<Task> onCancel = null,
            Action<Task> onOther = null)
        {
            task.ContinueWith(executedTask =>
            {
                if (!executedTask.IsFaulted && !executedTask.IsCanceled)
                {
                    onDone?.Invoke(task);
                    Console.WriteLine("Finished task, yay!");
                }

                if (executedTask.IsFaulted)
                {
                    onFault?.Invoke(task, executedTask.Exception);
                    Console.WriteLine("Failed task, oh noes.");
                }

                if (executedTask.IsCanceled)
                {
                    onCancel?.Invoke(task);
                    Console.WriteLine("Someone pushed stop...");
                }
            });

            return task.Id;
        }

        // EB: Perhaps naming this ContinueWith is more appropriate, since it's a conditional ContinueWith
        public static int FireAndHandleLater(
            this Task task,
            Func<Task, Task> onDone = null,
            Func<Task, AggregateException, Task> onFault = null,
            Func<Task, Task> onCancel = null,
            Func<Task, Task> onOther = null)
        {
            task.ContinueWith(async executedTask =>
            {
                if (!executedTask.IsFaulted && !executedTask.IsCanceled)
                    if (onDone != null)
                        await onDone(task);

                if (executedTask.IsFaulted)
                    if (onFault != null)
                        await onFault(task, executedTask.Exception);

                if (executedTask.IsCanceled)
                    if (onCancel != null)
                        await onCancel(task);
            });
            return task.Id;
        }

        // EB: Perhaps naming this ContinueWith is more appropriate, since it's a conditional ContinueWith
        public static int FireAndHandleLater<TResult>(this Task<TResult> task,
            Action<Task, TResult> onDone = null,
            Action<Task, AggregateException> onFault = null,
            Action<Task> onCancel = null,
            Action<Task> onOther = null)
        {
            task.ContinueWith(executedTask =>
            {
                if (!executedTask.IsFaulted && !executedTask.IsCanceled)
                {
                    onDone?.Invoke(task, executedTask.Result);
                    Console.WriteLine("Finished task, yay!");
                }

                if (executedTask.IsFaulted)
                {
                    onFault?.Invoke(task, executedTask.Exception);
                    Console.WriteLine("Failed task, oh noes.");
                }

                if (executedTask.IsCanceled)
                {
                    onCancel?.Invoke(task);
                    Console.WriteLine("Someone pushed stop...");
                }
            });

            return task.Id;
        }

        // EB: Perhaps naming this ContinueWith is more appropriate, since it's a conditional ContinueWith
        public static int FireAndHandleLater<TResult>(
            this Task<TResult> task,
            Func<Task, TResult, Task> onDone = null,
            Func<Task, AggregateException, Task> onFault = null,
            Func<Task, Task> onCancel = null,
            Func<Task, Task> onOther = null)
        {
            task.ContinueWith(async executedTask =>
            {
                if (!executedTask.IsFaulted && !executedTask.IsCanceled)
                    if (onDone != null)
                        await onDone(task, executedTask.Result);

                if (executedTask.IsFaulted)
                    if (onFault != null)
                        await onFault(task, executedTask.Exception);

                if (executedTask.IsCanceled)
                    if (onCancel != null)
                        await onCancel(task);
            });

            return task.Id;
        }
    }
}