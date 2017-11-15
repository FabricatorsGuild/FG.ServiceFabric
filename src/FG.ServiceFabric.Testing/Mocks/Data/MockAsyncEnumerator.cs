using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data;

namespace FG.ServiceFabric.Testing.Mocks.Data
{
	/// <summary>
	///     Simply wrapper for a synchronous IEnumerator of T.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	internal class MockAsyncEnumerator<T> : IAsyncEnumerator<T>
	{
		private readonly IEnumerator<T> enumerator;

		public MockAsyncEnumerator(IEnumerator<T> enumerator)
		{
			this.enumerator = enumerator;
		}


		public T Current
		{
			get { return this.enumerator.Current; }
		}

		public void Dispose()
		{
			this.enumerator.Dispose();
		}

		public Task<bool> MoveNextAsync(CancellationToken cancellationToken)
		{
			return Task.FromResult(this.enumerator.MoveNext());
		}

		public void Reset()
		{
			this.enumerator.Reset();
		}
	}
}