using System.Collections.Generic;
using System.Collections.Immutable;

namespace Vultus.Search
{
    public interface IIndex<TKey, TItem>
    {
        long Count { get; }
        ImmutableHashSet <TKey> Keys { get; }
        void Update(IEnumerable<TItem> items);
        IEnumerable<TItem> Items();
        TItem Filter(TKey lookup);
        IEnumerable<TItem> Filter(IEnumerable<TKey>? lookups);
    }
}
