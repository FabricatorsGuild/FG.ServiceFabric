﻿using System;
using System.Threading;

namespace FG.ServiceFabric.Services.Runtime.StateSession
{
	public class RwLock
	{
		private readonly ReaderWriterLockSlim rwLock;

		public RwLock()
		{
			this.rwLock = new ReaderWriterLockSlim();
		}

		public IDisposable AcquireWriteLock()
		{
			return new DisposableWriteLock(this.rwLock);
		}

		public IDisposable AcquireReadLock()
		{
			return new DisposableReadLock(this.rwLock);
		}

		private abstract class DisposableLockBase : IDisposable
		{
			private readonly ReaderWriterLockSlim rwLock;
			private bool isDisposed;

			protected ReaderWriterLockSlim Lock
			{
				get { return this.rwLock; }
			}

			protected DisposableLockBase(ReaderWriterLockSlim rwLock)
			{
				this.rwLock = rwLock;
				this.isDisposed = false;
			}

			void IDisposable.Dispose()
			{
				this.Dispose(true);
				GC.SuppressFinalize(this);
			}

			protected virtual void Dispose(bool disposing)
			{
				if (!this.isDisposed)
				{
					this.OnDispose();

					// ignore disposing flag - no native resources
				}

				this.isDisposed = true;
			}

			protected abstract void OnDispose();

			~DisposableLockBase()
			{
				this.Dispose(false);
			}
		};

		private class DisposableReadLock : DisposableLockBase
		{
			public DisposableReadLock(ReaderWriterLockSlim rwLock)
				: base(rwLock)
			{
				this.Lock.EnterReadLock();
			}

			protected override void OnDispose()
			{
				this.Lock.ExitReadLock();
			}
		};

		private class DisposableWriteLock : DisposableLockBase
		{
			public DisposableWriteLock(ReaderWriterLockSlim rwLock)
				: base(rwLock)
			{
				this.Lock.EnterWriteLock();
			}

			protected override void OnDispose()
			{
				this.Lock.ExitWriteLock();
			}
		};
	}
}