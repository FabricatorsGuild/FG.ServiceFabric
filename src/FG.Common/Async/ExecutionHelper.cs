using System;
using System.Threading;
using System.Threading.Tasks;

namespace FG.Common.Async
{
	public static class ExecutionHelper
	{
		public static async Task<TResult> ExecuteWithRetriesAsync<TResult>(Func<CancellationToken, Task<TResult>> func,
			int maxRetries, TimeSpan retryDelay, CancellationToken userCancellationToken)
		{
			TResult result;
			var retries = 0;
			while (true)
			{
				try
				{
					result = await func(userCancellationToken);
					break;
				}
				catch (OperationCanceledException)
				{
					if (userCancellationToken.IsCancellationRequested)
						throw;
				}
				catch (Exception)
				{
					if (retries == maxRetries)
						throw;
				}
				await Task.Delay(retryDelay, userCancellationToken);
				retries++;
			}
			return result;
		}

		public static async Task ExecuteWithRetriesAsync(Func<CancellationToken, Task> func, int maxRetries,
			TimeSpan retryDelay, CancellationToken userCancellationToken)
		{
			var retries = 0;
			while (true)
			{
				try
				{
					await func(userCancellationToken);
					break;
				}
				catch (OperationCanceledException)
				{
					if (userCancellationToken.IsCancellationRequested)
						throw;
				}
				catch (Exception)
				{
					if (retries == maxRetries)
						throw;
				}
				await Task.Delay(retryDelay, userCancellationToken);
				retries++;
			}
			return;
		}
	}
}