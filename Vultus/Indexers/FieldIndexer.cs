using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Vultus.Search.Indexers
{
    /// <summary>
    /// Indexes TItem by TProperty and TKey allowing callers to lookup TKey by TProperty
    /// </summary>
    /// <typeparam name="TProperty">Property to be indexed</typeparam>
    /// <typeparam name="TKey">Unique key of item</typeparam>
    /// <typeparam name="TItem">Item to index</typeparam>
    public class FieldIndexer<TProperty, TKey, TItem> : IIndexer<TProperty, TKey, TItem>
    {
        internal readonly SemaphoreSlim _semaphore;
        internal readonly Func<TItem, TKey> _getKey;
        internal readonly Func<TItem, TProperty> _getProperty;
        internal Dictionary<TProperty, HashSet<TKey>?> _index;

        public FieldIndexer(Func<TItem, TKey> getKey, Func<TItem, TProperty> getProperty)
        {
            _semaphore = new SemaphoreSlim(1, 1);
            _getKey = getKey;
            _getProperty = getProperty;
            _index = new Dictionary<TProperty, HashSet<TKey>?>();
        }

        public void Update(IEnumerable<TItem> values)
        {
            if (!values.Any())
                return;

            _semaphore.Wait();
            try
            {
                // Let's assume we're going to index at least 2 different values, so take half the count and set our initial HashSet capacity to that to avoid over allocations
                int maxSize = values.Count() / 2;
                var updatedIndex = new Dictionary<TProperty, HashSet<TKey>?>();

                foreach (var item in values)
                {
                    var key = _getKey(item);
                    var property = _getProperty(item);
                    if (property != null)
                    {
                        if (updatedIndex.ContainsKey(property))
                        {
                            updatedIndex[property].Add(key);
                        }
                        else
                        {
                            updatedIndex.Add(property, new HashSet<TKey>(maxSize) {key});
                        }
                    }
                }

                // thread safe swap index with updated one
                Interlocked.Exchange(ref _index, updatedIndex);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public HashSet<TKey> Items()
        {
            return _index.Values.SelectMany(x => x).ToHashSet();
        }

        public HashSet<TKey>? Filter(TProperty lookup)
        {
            if (_index.ContainsKey(lookup))
            {
                return _index[lookup];
            }

            return null;
        }

        public HashSet<TKey>? Filter(IEnumerable<TProperty> lookups)
        {
            return lookups.Where(x => _index.ContainsKey(x)).SelectMany(x => _index[x]).ToHashSet<TKey>();
        }

        public HashSet<TKey>? Filter(object lookup)
        {
            return Filter((TProperty)lookup);
        }

        public HashSet<TKey>? Filter(IEnumerable<object> lookups)
        {
            if (!lookups.Any())
                return null;

            return Filter(lookups.Cast<TProperty>());
        }
    }
}
