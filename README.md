<div id="top"></div>

<br />
<div align="center">

<h3 align="center">Vultus Search</h3>

  <p align="center">
    Simple, lightweight, in-memory search index
    <br />
    <br />
    <a href="https://github.com/bagal/vultus/issues">Report Bug</a>
    ·
    <a href="https://github.com/bagal/vultus/issues">Request Feature</a>
  </p>
</div>

<!-- ABOUT THE PROJECT -->

## Overview

Vultus is a lightweight search index for .NET, using the concept of indexes over an in-memory key-value store i.e. a Dictionary<Key, Value>.

Out of the box it supports a property field indexer, allowing you to create property value buckets that you can easily filter on.

You can add your own custom Indexers by implementing the IIndexer interface.

<p align="right">(<a href="#top">back to top</a>)</p>

## Installation

Vultus is available as a NuGet package. You can install it using the NuGet Package Console window:

```
PM> Install-Package Vultus.Search
```

<p align="right">(<a href="#top">back to top</a>)</p>

<!-- USAGE EXAMPLES -->

## Usage

### Creating and populating an index

```
var index = new Index<string, MyObject>(x => x.Key);

var items = new List<MyObject> {
  new MyObject { Key = "item1", Property = "property1" },
  new MyObject { Key = "item2", Property = "property2" }
};

index.Update(items);
```

### Adding a field indexer and filtering

```
// Add indexer for "Property"
var indexByProp = index.AddIndex("MyPropIndex", x => x.Property);

// Filter on the indexer will return a list of matching keys
var keys = indexByProp.Filter("property1");

// We can then lookup our values from the index using the keys
var items = index.Filter(keys);
```

### Filtering over multiple indexers

```
// Add some indexers for titles and UK counties
var indexByTitle = index.AddIndex("Title", x => x.Title); // Mrs, Mr, Miss, Ms
var indexByCounty = index.AddIndex("County", x => x.County); // London, Yorkshire, Merseyside, Devon etc.

// Find matches in our indexers
var titles = indexByTitle.Filter("Ms");
var counties = indexByCounty.Filter("London");

// Now lookup items where the result is both a match on the title and the county i.e. Ms and London
var items = index.Filter(titles.Intersect(counties));
```

<p align="right">(<a href="#top">back to top</a>)</p>

<!-- LICENSE -->

## License

Distributed under the MIT License. See `LICENSE` for more information.

<p align="right">(<a href="#top">back to top</a>)</p>

<!-- ACKNOWLEDGMENTS -->

## Acknowledgments

Inspired by the day job and the following articles and repositories:

- [Indexer - Index large collections by different keys on memory or disk](https://www.codeproject.com/Articles/563200/Indexer-Index-large-collections-by-different-keys)
- [Multiple indexes over an in memory collection for faster search](https://codereview.stackexchange.com/questions/40811/multiple-indexes-over-an-in-memory-collection-for-faster-search)
- [Indexing In-Memory Collections For Blazing Fast Access](https://www.c-sharpcorner.com/article/indexing-in-memory-collections-for-blazing-fast-access/)
  - [CodexMicroORM - Package used in above blog](https://github.com/codexguy/CodexMicroORM)

<p align="right">(<a href="#top">back to top</a>)</p>
