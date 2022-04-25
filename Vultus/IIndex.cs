using System.Collections.Generic;

namespace Vultus.Search
{
    public interface IIndex<TKey, TItem>
    {
        long Count { get; }
        IEnumerable<TKey> Keys { get; }
        IEnumerable<TItem> Items { get; }
        void Update(IEnumerable<TItem> items, IEnumerable<TKey>? toRemove = null);
        bool ContainsKey(TKey lookup);
        TItem this[TKey lookup] { get; }
        TItem Filter(TKey lookup);
        IEnumerable<TItem> Filter(IEnumerable<TKey>? lookups);
    }
}
