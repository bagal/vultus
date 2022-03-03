using System.Collections.Generic;

namespace Vultus.Search.Indexers
{
    public interface IIndexer<TKey, in TItem>
    {
        void Update(IEnumerable<TItem> values);
        HashSet<TKey> Items();
        HashSet<TKey>? Filter(object lookup);
        HashSet<TKey>? Filter(IEnumerable<object> lookups);
    }

    public interface IIndexer<in TProperty, TKey, in TItem> : IIndexer<TKey, TItem>
    {
        HashSet<TKey>? Filter(TProperty lookup);
        HashSet<TKey>? Filter(IEnumerable<TProperty> lookups);
    }
}
