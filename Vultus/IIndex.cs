using System.Collections.Generic;

namespace Vultus.Search
{
    public interface IIndex<TKey, TItem>
    {
        void Update(IEnumerable<TItem> items);
        IEnumerable<TItem> Items();
        TItem Filter(TKey lookup);
        IEnumerable<TItem> Filter(IEnumerable<TKey>? lookups);
    }
}
