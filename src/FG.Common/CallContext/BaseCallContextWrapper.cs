namespace FG.Common.CallContext
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class BaseCallContextWrapper<TBaseCallContext, TValueType> : IDisposable
        where TBaseCallContext : BaseCallContext<TBaseCallContext, TValueType>
    {
        private readonly IEnumerable<KeyValuePair<string, TValueType>> previousProperties = Enumerable.Empty<KeyValuePair<string, TValueType>>();

        public BaseCallContextWrapper(TBaseCallContext context, IEnumerable<KeyValuePair<string, TValueType>> previousProperties)
        {
            this.Context = context;
            this.previousProperties = previousProperties ?? throw new ArgumentNullException(nameof(previousProperties));
        }

        public BaseCallContextWrapper(TBaseCallContext context)
        {
            this.Context = context;
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
        public IEnumerable<string> Keys => this.Context?.Keys ?? Array.Empty<string>();

        protected TBaseCallContext Context { get; }

        protected bool ShouldDispose { get; set; }

        /// <summary>
        ///     Disposes the instance
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Gets all property names/keys
        /// </summary>
        /// <returns>The property names/keys</returns>
        public IEnumerable<string> GetAllKeys()
        {
            return this.Keys;
        }

        private void Dispose(bool disposing)
        {
            if (disposing && this.ShouldDispose)
            {
                this.Context?.Update(d => d.Clear().AddRange(this.previousProperties));
            }
        }
    }
}