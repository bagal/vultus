using Vultus.Search.Indexers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Vultus.Search
{
    /// <summary>
    /// Indexes TItem by TKey and maintains a list of indexes over TItem
    /// </summary>
    /// <typeparam name="TKey">Unique key of TItem</typeparam>
    /// <typeparam name="TItem">Item to index</typeparam>
    public class Index<TKey, TItem> : IIndex<TKey, TItem> where TItem : class
    {
        private readonly SemaphoreSlim _semaphore;
        internal readonly Func<TItem, TKey> _getKey;
        internal Dictionary<TKey, TItem> _index;
        internal Dictionary<string, IIndexer<TKey, TItem>> _indexes;
        internal readonly IEqualityComparer<TKey>? _comparer = null;

        /// <summary>
        /// Initializes a new index over TItem using the provided key func using the default comparer for TKey
        /// </summary>
        /// <param name="getKey">Func to return the unique key for TItem</param>
        public Index(Func<TItem, TKey> getKey) 
        {
            _semaphore = new SemaphoreSlim(1, 1);
            _getKey = getKey;
            _index = new Dictionary<TKey, TItem>();
            _indexes = new Dictionary<string, IIndexer<TKey, TItem>>();
        }

        /// <summary>
        /// Initializes a new index over TItem using the provided key func using a custom comparer for TKey
        /// </summary>
        /// <param name="getKey">Func to return the unique key for TItem</param>
        /// <param name="comparer">Custom comparer to use for TKey</param>
        public Index(Func<TItem, TKey> getKey, IEqualityComparer<TKey> comparer)
        {
            _semaphore = new SemaphoreSlim(1, 1);
            _getKey = getKey;
            _index = new Dictionary<TKey, TItem>(comparer);
            _indexes = new Dictionary<string, IIndexer<TKey, TItem>>();
            _comparer = comparer;
        }

        /// <summary>
        /// Number of items contained in the index
        /// </summary>
        public long Count => _index.Count;

        /// <summary>
        /// All keys in the index
        /// </summary>
        public IEnumerable<TKey> Keys => _index.Keys;

        /// <summary>
        /// All items in the index
        /// </summary>
        public IEnumerable<TItem> Items => _index.Values;

        /// <summary>
        /// Updates the index (and all indexers) with additional items or items no longer valid
        /// </summary>
        /// <param name="items">Items to add or update in the index</param>
        /// <param name="toRemove">Items to remove from the index</param>
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

        /// <summary>
        /// Determines whether the index contains the specified key
        /// </summary>
        /// <param name="lookup">Key to filter on</param>
        /// <returns>bool</returns>
        public bool ContainsKey(TKey lookup) => _index.ContainsKey(lookup);

        /// <summary>
        /// Filter index on single key value using an indexer
        /// </summary>
        /// <param name="lookup">Key to filter on</param>
        /// <returns>TItem</returns>
        public TItem this[TKey lookup] => _index[lookup];

        /// <summary>
        /// Filter index on single key value
        /// </summary>
        /// <param name="lookup">Key to filter on</param>
        /// <returns>TItem or default</returns>
        public TItem Filter(TKey lookup)
        {
            if (_index.ContainsKey(lookup))
            {
                return _index[lookup];
            }

            return default!;
        }

        /// <summary>
        /// Filter index on key values
        /// </summary>
        /// <param name="lookups">Key values to filter by</param>
        /// <returns>IEnumerable<TItem></returns>
        public IEnumerable<TItem> Filter(IEnumerable<TKey>? lookups)
        {
            return lookups.Where(x => _index.ContainsKey(x)).Select(x => _index[x]).ToList();
        }

        /// <summary>
        /// Add a new indexer
        /// </summary>
        /// <param name="name">Unique name of this index</param>
        /// <param name="index">The indexer to add</param>
        /// <returns>Newly added indexer</returns>
        /// <exception cref="ArgumentNullException">Name is a required field</exception>
        /// <exception cref="ArgumentException">Name must be unique</exception>
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

        /// <summary>
        /// Adds a new FieldIndexer
        /// </summary>
        /// <typeparam name="TProperty">Property to be indexed</typeparam>
        /// <param name="name">Unique name of this index</param>
        /// <param name="extractProperty">Func to extract property value from object</param>
        /// <returns>Newly created indexer</returns>
        public IIndexer<TKey, TItem> AddIndex<TProperty>(string name, Func<TItem, TProperty> extractProperty)
        {
            var index = new FieldIndexer<TProperty, TKey, TItem>(_getKey, extractProperty);

            return AddIndex(name, index);
        }

        /// <summary>
        /// Add a new FieldIndexer using a custom property comparer such as StringComparer
        /// </summary>
        /// <typeparam name="TProperty">Property to be indexed</typeparam>
        /// <param name="name">Unique name of this index</param>
        /// <param name="extractProperty">Func to extract property value from object</param>
        /// <param name="comparer">Property comparer to use</param>
        /// <returns>Newly created indexer</returns>
        public IIndexer<TKey, TItem> AddIndex<TProperty>(string name, Func<TItem, TProperty> extractProperty, IEqualityComparer<TProperty> comparer)
        {
            var index = new FieldIndexer<TProperty, TKey, TItem>(_getKey, extractProperty, comparer);

            return AddIndex(name, index);
        }

        /// <summary>
        /// Add a new MultiValueFieldIndexer
        /// </summary>
        /// <typeparam name="TProperty">Property to be indexed</typeparam>
        /// <param name="name">Unique name of this index</param>
        /// <param name="extractProperty">Func to extract multiple property values from object</param>
        /// <returns>Newly created indexer</returns>
        public IIndexer<TKey, TItem> AddMultiValueIndex<TProperty>(string name, Func<TItem, IEnumerable<TProperty>> extractProperty)
        {
            var index = new MultiValueFieldIndexer<TProperty, TKey, TItem>(_getKey, extractProperty);

            return AddIndex(name, index);
        }

        /// <summary>
        /// Add a new MultiValueFieldIndexer using a custom property comparer such as StringComparer
        /// </summary>
        /// <typeparam name="TProperty">Property to be indexed</typeparam>
        /// <param name="name">Unique name of this index</param>
        /// <param name="extractProperty">Func to extract multiple property values from object</param>
        /// <param name="comparer">Property comparer to use</param>
        /// <returns>Newly created indexer</returns>
        public IIndexer<TKey, TItem> AddMultiValueIndex<TProperty>(string name, Func<TItem, IEnumerable<TProperty>> extractProperty, IEqualityComparer<TProperty> comparer)
        {
            var index = new MultiValueFieldIndexer<TProperty, TKey, TItem>(_getKey, extractProperty, comparer);

            return AddIndex(name, index);
        }
    }
}
