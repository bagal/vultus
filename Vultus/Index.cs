using Vultus.Search.Indexers;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        internal ImmutableHashSet<TKey> _keys;
        internal ImmutableList<TItem> _items;
        internal Dictionary<string, IIndexer<TKey, TItem>> _indexes;

        public Index(Func<TItem, TKey> getKey) 
        {
            _semaphore = new SemaphoreSlim(1, 1);
            _getKey = getKey;
            _index = new Dictionary<TKey, TItem>();
            _keys = ImmutableHashSet.Create<TKey>();
            _items = ImmutableList<TItem>.Empty;
            _indexes = new Dictionary<string, IIndexer<TKey, TItem>>();
        }

        public long Count => _items.Count;

        public void Update(IEnumerable<TItem> items)
        {
            if (!items.Any())
                return;

            _semaphore.Wait();
            try
            {
                var updatedCache = new Dictionary<TKey, TItem>(_index);

                foreach (var item in items)
                {
                    var key = _getKey(item);
                    if (updatedCache.ContainsKey(key))
                    {
                        updatedCache[key] = item;
                    }
                    else
                    {
                        updatedCache.Add(key, item);
                    }
                }

                Interlocked.Exchange(ref _index, updatedCache);
                Interlocked.Exchange(ref _keys, _index.Keys.ToImmutableHashSet());
                Interlocked.Exchange(ref _items, _index.Values.ToImmutableList());

                Parallel.ForEach(_indexes, (x) => x.Value.Update(_items));
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public ImmutableHashSet<TKey> Keys => _keys;

        public IEnumerable<TItem> Items()
        {
            return _items;
        }

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
            if (_indexes.ContainsKey(name))
                throw new ArgumentException($"{nameof(name)} already exists", nameof(name));

            _semaphore.Wait();
            try
            {
                _indexes.Add(name, index);

                index.Update(Items());
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
    }
}
