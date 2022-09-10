using Vultus.Search.Indexers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Vultus.Search
{
    public class Index<TKey, TItem> : IIndex<TKey, TItem> where TItem : class
    {
        private readonly SemaphoreSlim _semaphore;
        internal readonly Func<TItem, TKey> _getKey;
        internal Dictionary<TKey, TItem> _index;
        internal Dictionary<string, IIndexer<TKey, TItem>> _indexes;
        internal readonly IEqualityComparer<TKey>? _comparer = null;

        public Index(Func<TItem, TKey> getKey) 
        {
            _semaphore = new SemaphoreSlim(1, 1);
            _getKey = getKey;
            _index = new Dictionary<TKey, TItem>();
            _indexes = new Dictionary<string, IIndexer<TKey, TItem>>();
        }

        public Index(Func<TItem, TKey> getKey, IEqualityComparer<TKey> comparer)
        {
            _semaphore = new SemaphoreSlim(1, 1);
            _getKey = getKey;
            _index = new Dictionary<TKey, TItem>(comparer);
            _indexes = new Dictionary<string, IIndexer<TKey, TItem>>();
            _comparer = comparer;
        }

        public long Count => _index.Count;

        public IEnumerable<TKey> Keys => _index.Keys;

        public IEnumerable<TItem> Items => _index.Values;

        public void Update(IEnumerable<TItem> items, IEnumerable<TKey>? toRemove = null)
        {
            if (items == null || !items.Any())
                return;

            _semaphore.Wait();
            try
            {
                var updatedCache = _comparer != null ? new Dictionary<TKey, TItem>(_index, _comparer) : new Dictionary<TKey, TItem>(_index);

                foreach (var item in items)
                {
                    var key = _getKey(item);
                    updatedCache[key] = item;
                }

                if (toRemove != null)
                {
                    foreach (var item in toRemove)
                    {
                        if (updatedCache.ContainsKey(item))
                        {
                            updatedCache.Remove(item);
                        }
                    }
                }

                Interlocked.Exchange(ref _index, updatedCache);

                Parallel.ForEach(_indexes, (x) => x.Value.Update(Items));
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public bool ContainsKey(TKey lookup) => _index.ContainsKey(lookup);

        public TItem this[TKey lookup] => _index[lookup];

        public TItem Filter(TKey lookup)
        {
            if (_index.ContainsKey(lookup))
            {
                return _index[lookup];
            }

            return default!;
        }

        public IEnumerable<TItem> Filter(IEnumerable<TKey>? lookups)
        {
            return lookups.Where(x => _index.ContainsKey(x)).Select(x => _index[x]).ToList();
        }

        public IIndexer<TKey, TItem> AddIndex(string name, IIndexer<TKey, TItem> index)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));
            if (_indexes.ContainsKey(name))
                throw new ArgumentException($"{nameof(name)} already exists", nameof(name));

            _semaphore.Wait();
            try
            {
                _indexes.Add(name, index);

                index.Update(Items);
            }
            finally
            {
                _semaphore.Release();
            }

            return index;
        }

        public IIndexer<TKey, TItem> AddIndex<TProperty>(string name, Func<TItem, TProperty> extractProperty)
        {
            var index = new FieldIndexer<TProperty, TKey, TItem>(_getKey, extractProperty);

            return AddIndex(name, index);
        }

        public IIndexer<TKey, TItem> AddIndex<TProperty>(string name, Func<TItem, TProperty> extractProperty, IEqualityComparer<TProperty> comparer)
        {
            var index = new FieldIndexer<TProperty, TKey, TItem>(_getKey, extractProperty, comparer);

            return AddIndex(name, index);
        }
    }
}
