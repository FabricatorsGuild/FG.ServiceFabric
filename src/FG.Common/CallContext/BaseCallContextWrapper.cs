using System;
using System.Collections.Generic;
using System.Linq;

namespace FG.Common.CallContext
{
    public class BaseCallContextWrapper<TBaseCallContext, TValueType> : IDisposable
        where TBaseCallContext : BaseCallContext<TBaseCallContext, TValueType>
    {
        private readonly IEnumerable<KeyValuePair<string, TValueType>> previousProperties =
            Enumerable.Empty<KeyValuePair<string, TValueType>>();

        public BaseCallContextWrapper(TBaseCallContext context,
            IEnumerable<KeyValuePair<string, TValueType>> previousProperties)
        {
            Context = context;
            this.previousProperties = previousProperties ?? throw new ArgumentNullException(nameof(previousProperties));
        }

        public BaseCallContextWrapper(TBaseCallContext context)
        {
            Context = context;
        }

        public BaseCallContextWrapper(IEnumerable<KeyValuePair<string, TValueType>> previousProperties)
        {
            this.previousProperties = previousProperties ?? throw new ArgumentNullException(nameof(previousProperties));
        }

        public BaseCallContextWrapper()
        {
        }

        /// <summary>
        ///     Gets all property names/keys
        /// </summary>
        public IEnumerable<string> Keys => Context?.Keys ?? Array.Empty<string>();

        protected TBaseCallContext Context { get; }

        protected bool ShouldDispose { get; set; }

        /// <summary>
        ///     Disposes the instance
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Gets all property names/keys
        /// </summary>
        /// <returns>The property names/keys</returns>
        public IEnumerable<string> GetAllKeys()
        {
            return Keys;
        }

        private void Dispose(bool disposing)
        {
            if (disposing && ShouldDispose)
                Context?.Update(d => d.Clear().AddRange(previousProperties));
        }
    }
}