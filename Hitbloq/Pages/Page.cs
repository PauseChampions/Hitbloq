using System.Collections.Generic;

namespace Hitbloq.Pages
{
    internal abstract class Page<T, TU>
    {
        protected abstract T Source { get; }
        public abstract IReadOnlyList<TU> Entries { get; }
    }
}