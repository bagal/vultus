using System.Collections.Generic;

namespace Vultus.Search.Indexers
{
    public interface IIndexer<TKey, in TItem>
    {
        void Update(IEnumerable<TItem> values);
        HashSet<TKey> Items();
        bool ContainsKey(object lookup);
        HashSet<TKey>? this[object lookup] { get; }
        HashSet<TKey>? Filter(object? lookup);
        HashSet<TKey>? Filter(IEnumerable<object>? lookups);
    }

    public interface IIndexer<in TProperty, TKey, in TItem> : IIndexer<TKey, TItem>
    {
        bool ContainsKey(TProperty lookup);
        HashSet<TKey>? this[TProperty lookup] { get; }
        HashSet<TKey>? Filter(TProperty lookup);
        HashSet<TKey>? Filter(IEnumerable<TProperty> lookups);
    }
}
