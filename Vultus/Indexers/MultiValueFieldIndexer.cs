using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Vultus.Search.Indexers
{
    /// <summary>
    /// Indexes TItem by TProperty and TKey allowing callers to lookup TKey by TProperty
    /// 
    /// Supports TItem containing multiple TProperty i.e. TKey can be in multiple buckets and therefore
    /// you can lookup TKey via different values of TProperty
    /// </summary>
    /// <typeparam name="TProperty">Property to be indexed</typeparam>
    /// <typeparam name="TKey">Unique key of item</typeparam>
    /// <typeparam name="TItem">Item to index</typeparam>
    public class MultiValueFieldIndexer<TProperty, TKey, TItem> : IIndexer<TProperty, TKey, TItem>
    {
        internal static readonly HashSet<TKey> EmptyHashSet = new HashSet<TKey>();
        internal readonly SemaphoreSlim _semaphore;
        internal readonly Func<TItem, TKey> _getKey;
        internal readonly Func<TItem, IEnumerable<TProperty>> _getProperties;
        internal Dictionary<TProperty, HashSet<TKey>?> _index;
        internal readonly IEqualityComparer<TProperty>? _comparer = null;

        public MultiValueFieldIndexer(Func<TItem, TKey> getKey, Func<TItem, IEnumerable<TProperty>> getProperties)
        {
            _semaphore = new SemaphoreSlim(1, 1);
            _getKey = getKey;
            _getProperties = getProperties;
            _index = new Dictionary<TProperty, HashSet<TKey>?>();
        }

        public MultiValueFieldIndexer(Func<TItem, TKey> getKey, Func<TItem, IEnumerable<TProperty>> getProperties, IEqualityComparer<TProperty> comparer)
        {
            _semaphore = new SemaphoreSlim(1, 1);
            _getKey = getKey;
            _getProperties = getProperties;
            _index = new Dictionary<TProperty, HashSet<TKey>?>(comparer);
            _comparer = comparer;
        }

        public void Update(IEnumerable<TItem> values)
        {
            if (values == null || !values.Any())
                return;

            _semaphore.Wait();
            try
            {
                // Let's assume we're going to index at least 2 different values, so take half the count and set our initial HashSet capacity to that to avoid over allocations
                int maxSize = values.Count() / 2;
                var updatedIndex = _comparer != null ? new Dictionary<TProperty, HashSet<TKey>?>(_comparer) : new Dictionary<TProperty, HashSet<TKey>?>();

                foreach (var item in values)
                {
                    var key = _getKey(item);
                    var properties = _getProperties(item);
                    if (properties?.Any() == true)
                    {
                        foreach (var property in properties)
                        {
                            if (updatedIndex.ContainsKey(property))
                            {
                                updatedIndex[property]!.Add(key);
                            }
                            else
                            {
                                updatedIndex.Add(property, new HashSet<TKey>(maxSize) { key });
                            }
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

        public bool ContainsKey(TProperty lookup) => _index.ContainsKey(lookup);
        public bool ContainsKey(object lookup) => _index.ContainsKey((TProperty)lookup);

        public HashSet<TKey>? this[TProperty lookup] => _index[lookup];
        public HashSet<TKey>? this[object lookup] => _index[(TProperty)lookup];

        public HashSet<TKey>? Filter(TProperty lookup)
        {
            if (lookup == null)
                return EmptyHashSet;

            if (_index.ContainsKey(lookup))
            {
                return _index[lookup];
            }

            return EmptyHashSet;
        }

        public HashSet<TKey>? Filter(IEnumerable<TProperty> lookups)
        {
            return lookups.Where(x => _index.ContainsKey(x)).SelectMany(x => _index[x]).ToHashSet<TKey>();
        }

        public HashSet<TKey>? Filter(object? lookup)
        {
            if (lookup == null)
                return EmptyHashSet;

            return Filter((TProperty)lookup);
        }

        public HashSet<TKey>? Filter(IEnumerable<object>? lookups)
        {
            if (lookups == null || !lookups.Any())
                return EmptyHashSet;

            return Filter(lookups.Cast<TProperty>());
        }
    }
}
