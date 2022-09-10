using System;
using Vultus.Search;
using Vultus.Tests.Search;
using Xunit;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using Xunit.Abstractions;
using System.Threading.Tasks;

namespace Vultus.Tests
{
    public class IndexTests
    {
        ITestOutputHelper _output;

        public IndexTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Should_Create_Index()
        {
            var result = new Index<string, TestObject>(x => x.Code);

            Assert.NotNull(result);
        }

        [Fact]
        public void Should_Add_Items_To_Index()
        {
            var index = new Index<string, TestObject>(x => x.Code);

            index.Update(MockData.GenerateTestObjects(20));

            Assert.Equal(20, index.Count);
        }

        [Fact]
        public void Should_Do_Nothing_If_Update_Empty()
        {
            var index = new Index<string, TestObject>(x => x.Code);

            index.Update(new List<TestObject>());

            Assert.Equal(0, index.Count);
        }

        [Theory]
        [InlineData("Test1", true)]
        [InlineData("Test2", false)]
        public void Index_ContainsKey(string key, bool expectedResult)
        {
            var index = new Index<string, TestObject>(x => x.Code);

            var test1 = new TestObject { Code = "Test1", Ccy = "GBP", Balance = 1000, High = true, Low = true };

            index.Update(new List<TestObject> { test1 });

            var result = index.ContainsKey(key);

            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData("tEsT1", true)]
        [InlineData("Test2", false)]
        public void Index_ContainsKey_Comparer(string key, bool expectedResult)
        {
            var index = new Index<string, TestObject>(x => x.Code, StringComparer.OrdinalIgnoreCase);

            var test1 = new TestObject { Code = "Test1", Ccy = "GBP", Balance = 1000, High = true, Low = true };

            index.Update(new List<TestObject> { test1 });

            var result = index.ContainsKey(key);

            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void Should_Filter_Items_By_Key()
        {
            var index = new Index<string, TestObject>(x => x.Code);

            index.Update(MockData.GenerateTestObjects(20));

            var result = index.Filter("Test10");

            Assert.NotNull(result);
            Assert.Equal("Test10", result.Code);
        }

        [Fact]
        public void Should_Lookup_Items_By_Key()
        {
            var index = new Index<string, TestObject>(x => x.Code);

            index.Update(MockData.GenerateTestObjects(20));

            var result = index["Test10"];

            Assert.NotNull(result);
            Assert.Equal("Test10", result.Code);
        }

        [Fact]
        public void Should_Filter_Items_By_Multiple_Keys()
        {
            var index = new Index<string, TestObject>(x => x.Code);

            index.Update(MockData.GenerateTestObjects(20));

            var result = index.Filter(new List<string> {"Test10", "Test11"});

            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.Equal("Test10", result.ElementAt(0).Code);
            Assert.Equal("Test11", result.ElementAt(1).Code);
        }

        [Fact]
        public void Should_Return_Null_Filter_Does_Not_Match()
        {
            var index = new Index<string, TestObject>(x => x.Code);

            index.Update(MockData.GenerateTestObjects(10));

            var result = index.Filter("Test11");

            Assert.Null(result);
        }

        [Fact]
        public void Should_Add_Indexer()
        {
            var index = new Index<string, TestObject>(x => x.Code);
            var indexer = index.AddIndex("test", x => x.Ccy);

            index.Update(MockData.GenerateTestObjects(20));

            Assert.NotNull(indexer);
            Assert.Equal(20, indexer.Items().Count);
        }

        [Fact]
        public void Should_Throw_When_Indexer_Already_Exists()
        {
            var index = new Index<string, TestObject>(x => x.Code);
            var indexer = index.AddIndex("test", x => x.Ccy);

            Assert.Throws<ArgumentException>(() => index.AddIndex("test", x => x.Ccy));
        }

        [Fact]
        public void Should_Throw_When_Indexer_Name_Is_Null()
        {
            var index = new Index<string, TestObject>(x => x.Code);
            
            Assert.Throws<ArgumentNullException>(() => index.AddIndex(null, x => x.Ccy));
        }

        [Fact]
        public void Should_Add_Multiple_Indexers()
        {
            var index = new Index<string, TestObject>(x => x.Code);
            var indexByCcy = index.AddIndex("ccy", x => x.Ccy);
            var indexByHigh = index.AddIndex("high", x => x.High);
            var indexByLow = index.AddIndex("low", x => x.Low);

            index.Update(MockData.GenerateTestObjects(20));

            Assert.NotNull(indexByCcy);
            Assert.NotNull(indexByHigh);
            Assert.NotNull(indexByLow);
            Assert.Equal(20, indexByCcy.Items().Count);
            Assert.Equal(20, indexByHigh.Items().Count);
            Assert.Equal(20, indexByLow.Items().Count);
        }

        [Fact]
        public void Indexer_Should_Filter()
        {
            var index = new Index<string, TestObject>(x => x.Code);
            var indexer = index.AddIndex("test", x => x.Ccy);

            var test1 = new TestObject { Code = "Test1", Ccy = "GBP", Balance = 1000, High = true, Low = true };
            var test2 = new TestObject { Code = "Test2", Ccy = "GBP", Balance = 1000, High = false, Low = true };

            index.Update(new List<TestObject> { test1, test2 });

            var result = indexer.Filter("GBP");

            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public void Indexer_With_Comparer_Should_Filter()
        {
            var index = new Index<string, TestObject>(x => x.Code);
            var indexer = index.AddIndex("test", x => x.Ccy, StringComparer.OrdinalIgnoreCase);

            var test1 = new TestObject { Code = "Test1", Ccy = "GBP", Balance = 1000, High = true, Low = true };
            var test2 = new TestObject { Code = "Test2", Ccy = "GBP", Balance = 1000, High = false, Low = true };

            index.Update(new List<TestObject> { test1, test2 });

            var result = indexer.Filter("gBp");

            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }


        [Fact]
        public void Indexer_Should_Lookup_By_Property()
        {
            var index = new Index<string, TestObject>(x => x.Code);
            var indexer = index.AddIndex("test", x => x.Ccy);

            var test1 = new TestObject { Code = "Test1", Ccy = "GBP", Balance = 1000, High = true, Low = true };
            var test2 = new TestObject { Code = "Test2", Ccy = "GBP", Balance = 1000, High = false, Low = true };

            index.Update(new List<TestObject> { test1, test2 });

            var result = indexer["GBP"];

            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }

        [Theory]
        [InlineData("GBP", true)]
        [InlineData("EUR", false)]
        public void Indexer_ContainsKey(string key, bool expectedResult)
        {
            var index = new Index<string, TestObject>(x => x.Code);
            var indexer = index.AddIndex("test", x => x.Ccy);

            var test1 = new TestObject { Code = "Test1", Ccy = "GBP", Balance = 1000, High = true, Low = true };

            index.Update(new List<TestObject> { test1 });

            var result = indexer.ContainsKey(key);

            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void Indexer_Should_Filter_Items_By_Multiple_Key_Objects()
        {
            var index = new Index<string, TestObject>(x => x.Code);
            var indexer = index.AddIndex("test", x => x.Ccy);

            var test1 = new TestObject { Code = "Test1", Ccy = "GBP", Balance = 1000, High = true, Low = true };
            var test2 = new TestObject { Code = "Test2", Ccy = "EUR", Balance = 1000, High = false, Low = true };

            index.Update(new List<TestObject> { test1, test2 });

            var result = indexer.Filter(new List<object> { "GBP", "EUR" });

            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.Equal("Test1", result.ElementAt(0));
            Assert.Equal("Test2", result.ElementAt(1));
        }

        [Fact]
        public void Indexer_Should_Return_Empty_HashSet_When_Empty_Lookups()
        {
            var index = new Index<string, TestObject>(x => x.Code);
            var indexer = index.AddIndex("test", x => x.Ccy);

            var test1 = new TestObject { Code = "Test1", Ccy = "GBP", Balance = 1000, High = true, Low = true };
            var test2 = new TestObject { Code = "Test2", Ccy = "EUR", Balance = 1000, High = false, Low = true };

            index.Update(new List<TestObject> { test1, test2 });

            var result = indexer.Filter(new List<object>());

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void Indexer_Should_Return_Empty_HashSet_When_Filter_Does_Not_Match()
        {
            var index = new Index<string, TestObject>(x => x.Code);
            var indexer = index.AddIndex("test", x => x.Ccy);

            var test1 = new TestObject { Code = "Test1", Ccy = "GBP", Balance = 1000, High = true, Low = true };
            var test2 = new TestObject { Code = "Test2", Ccy = "GBP", Balance = 1000, High = false, Low = true };

            index.Update(new List<TestObject> { test1, test2 });

            var result = indexer.Filter("EUR");

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void Should_Filter_Index_From_Indexer_Result()
        {
            var index = new Index<string, TestObject>(x => x.Code);
            var indexer = index.AddIndex("test", x => x.Ccy);

            index.Update(MockData.GenerateTestObjects(20));

            var result = index.Filter(indexer.Filter("GBP"));

            Assert.NotNull(result);
            Assert.True(result.Count() > 0);
        }

        [Fact]
        public void Should_Add_Multiple_Indexers_And_Filter()
        {
            var index = new Index<string, TestObject>(x => x.Code);
            var indexByCcy = index.AddIndex("ccy", x => x.Ccy);
            var indexByHigh = index.AddIndex("high", x => x.High);
            var indexByLow = index.AddIndex("low", x => x.Low);

            var testData = MockData.GenerateTestObjects(1000).ToList();
            index.Update(testData);

            var high = indexByHigh.Filter(true);
            var low = indexByLow.Filter(true);
            var ccy = indexByCcy.Filter("GBP");

            var ccyLookup = index.Filter(ccy).ToList();
            var highLookup = index.Filter(high).ToList();
            var lowLookup = index.Filter(low).ToList();
            var result = ccy.Intersect(high).Intersect(low).ToList();
            var lookup = index.Filter(result);

            Assert.NotNull(result);
            Assert.NotNull(lookup);
            Assert.True(ccyLookup.All(x => x.Ccy == "GBP"));
            Assert.True(highLookup.All(x => x.High == true));
            Assert.True(lowLookup.All(x => x.Low == true));
            Assert.True(lookup.All(x => x.Ccy == "GBP" && x.High == true && x.Low == true));
        }

        [Fact]
        public void Should_Filter_Multiple()
        {
            var index = new Index<string, TestObject>(x => x.Code);
            var indexByCcy = index.AddIndex("ccy", x => x.Ccy);
            var indexByHigh = index.AddIndex("high", x => x.High);
            var indexByLow = index.AddIndex("low", x => x.Low);

            var test1 = new TestObject { Code = "Test1", Ccy = "GBP", Balance = 1000, High = true, Low = true };
            var test2 = new TestObject { Code = "Test2", Ccy = "EUR", Balance = 1000, High = true, Low = false };
            var test3 = new TestObject { Code = "Test3", Ccy = "GBP", Balance = 1000, High = false, Low = true };

            index.Update(new List<TestObject> { test1, test2, test3 });

            var resultCcy = indexByCcy.Filter("GBP");
            var resultHigh = indexByHigh.Filter(true);
            var resultLow= indexByLow.Filter(true);

            var intersect = resultCcy.Intersect(resultHigh).Intersect(resultLow).ToList();

            var lookup = index.Filter(intersect).FirstOrDefault();

            Assert.NotNull(resultCcy);
            Assert.NotNull(resultHigh);
            Assert.NotNull(resultLow);
            Assert.NotNull(intersect);
            Assert.NotNull(lookup);
            Assert.Equal(2, resultCcy.Count());
            Assert.Equal(2, resultHigh.Count());
            Assert.Equal(2, resultLow.Count());
            Assert.Single(intersect);
            Assert.Equal("Test1", resultCcy.ElementAt(0));
            Assert.Equal("Test3", resultCcy.ElementAt(1));
            Assert.Equal("Test1", resultHigh.ElementAt(0));
            Assert.Equal("Test2", resultHigh.ElementAt(1));
            Assert.Equal("Test1", resultLow.ElementAt(0));
            Assert.Equal("Test3", resultLow.ElementAt(1));
            Assert.Equal("Test1", intersect.ElementAt(0));
            Assert.Equal(test1, lookup);
        }

        [Fact]
        public async Task Should_Filter_Multiple_Threads()
        {
            var index = new Index<string, TestObject>(x => x.Code);
            var indexByCcy = index.AddIndex("ccy", x => x.Ccy);
            var indexByHigh = index.AddIndex("high", x => x.High);
            var indexByLow = index.AddIndex("low", x => x.Low);

            var test1 = new TestObject { Code = "Test1", Ccy = "GBP", Balance = 1000, High = true, Low = true };
            var test2 = new TestObject { Code = "Test2", Ccy = "EUR", Balance = 1000, High = true, Low = false };
            var test3 = new TestObject { Code = "Test3", Ccy = "GBP", Balance = 1000, High = false, Low = true };

            var testUpdate1 = new TestObject { Code = "Test1", Ccy = "GBP", Balance = 500, High = false, Low = false };
            var testUpdate2 = new TestObject { Code = "Test2", Ccy = "EUR", Balance = 700, High = false, Low = true };
            var testUpdate3 = new TestObject { Code = "Test3", Ccy = "GBP", Balance = 300, High = true, Low = false };

            await Task.WhenAll(
                Task.Run(() => index.Update(new List<TestObject> { test1, test2, test3 })),
                Task.Run(() => index.Update(new List<TestObject> { testUpdate1, testUpdate2, testUpdate3 }))
            );

            var resultCcy = indexByCcy.Filter("GBP");
            var resultHigh = indexByHigh.Filter(true);
            var resultLow = indexByLow.Filter(true);

            var intersect = resultCcy.Intersect(resultHigh).Intersect(resultLow).ToList();

            var lookup = index.Filter(intersect).FirstOrDefault();

            Assert.NotNull(resultCcy);
            Assert.NotNull(resultHigh);
            Assert.NotNull(resultLow);
            Assert.NotNull(intersect);
            Assert.Null(lookup);
            Assert.Equal(2, resultCcy.Count());
            Assert.Single(resultHigh);
            Assert.Single(resultLow);
            Assert.Empty(intersect);
            Assert.Equal("Test1", resultCcy.ElementAt(0));
            Assert.Equal("Test3", resultCcy.ElementAt(1));
            Assert.Equal("Test3", resultHigh.ElementAt(0));
            Assert.Equal("Test2", resultLow.ElementAt(0));
        }

        [Theory]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(100000)]
        [InlineData(1000000)]
        public void Performance_Test_Add_And_Filter(int count)
        {
            var testData = MockData.GenerateTestObjects(count).ToList();

            var sw = new Stopwatch();
            sw.Start();
            var index = new Index<string, TestObject>(x => x.Code);
            var indexByCcy = index.AddIndex("ccy", x => x.Ccy);
            var indexByHigh = index.AddIndex("high", x => x.High);
            var indexByLow = index.AddIndex("low", x => x.Low);

            index.Update(testData);

            var high = indexByHigh.Filter(true);
            var low = indexByLow.Filter(true);
            var ccy = indexByCcy.Filter("GBP");

            var ccyLookup = index.Filter(ccy).ToList();
            var highLookup = index.Filter(high).ToList();
            var lowLookup = index.Filter(low).ToList();
            var result = ccy.Intersect(high).Intersect(low).ToList();
            var lookup = index.Filter(result);

            sw.Stop();
            var elapsed = sw.ElapsedMilliseconds;
            _output.WriteLine($"{elapsed}ms");

            Assert.NotNull(result);
            Assert.NotNull(lookup);
            Assert.True(ccyLookup.All(x => x.Ccy == "GBP"));
            Assert.True(highLookup.All(x => x.High == true));
            Assert.True(lowLookup.All(x => x.Low == true));
            Assert.True(lookup.All(x => x.Ccy == "GBP" && x.High == true && x.Low == true));
        }

        [Theory]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(100000)]
        [InlineData(1000000)]
        public void Performance_Test_Add_Update_And_Filter(int count)
        {
            var testData = MockData.GenerateTestObjects(count).ToList();
            var testData2 = MockData.GenerateTestObjects(count).ToList();

            var sw = new Stopwatch();
            sw.Start();
            var index = new Index<string, TestObject>(x => x.Code);
            var indexByCcy = index.AddIndex("ccy", x => x.Ccy);
            var indexByHigh = index.AddIndex("high", x => x.High);
            var indexByLow = index.AddIndex("low", x => x.Low);

            index.Update(testData);
            index.Update(testData2);

            var high = indexByHigh.Filter(true);
            var low = indexByLow.Filter(true);
            var ccy = indexByCcy.Filter("GBP");

            var ccyLookup = index.Filter(ccy).ToList();
            var highLookup = index.Filter(high).ToList();
            var lowLookup = index.Filter(low).ToList();
            var result = ccy.Intersect(high).Intersect(low).ToList();
            var lookup = index.Filter(result);

            sw.Stop();
            var elapsed = sw.ElapsedMilliseconds;
            _output.WriteLine($"{elapsed}ms");

            Assert.NotNull(result);
            Assert.NotNull(lookup);
            Assert.True(ccyLookup.All(x => x.Ccy == "GBP"));
            Assert.True(highLookup.All(x => x.High == true));
            Assert.True(lowLookup.All(x => x.Low == true));
            Assert.True(lookup.All(x => x.Ccy == "GBP" && x.High == true && x.Low == true));
        }

        [Theory]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(100000)]
        [InlineData(1000000)]
        public async Task Performance_Test_Add_Update_And_Filter_Multiple_Threads(int count)
        {
            var testData = MockData.GenerateTestObjects(count).ToList();
            var testData2 = MockData.GenerateTestObjects(count).ToList();

            var sw = new Stopwatch();
            sw.Start();
            var index = new Index<string, TestObject>(x => x.Code);
            var indexByCcy = index.AddIndex("ccy", x => x.Ccy);
            var indexByHigh = index.AddIndex("high", x => x.High);
            var indexByLow = index.AddIndex("low", x => x.Low);

            await Task.WhenAll(
                Task.Run(() => index.Update(testData)),
                Task.Run(() => index.Update(testData2))
            );

            var high = indexByHigh.Filter(true);
            var low = indexByLow.Filter(true);
            var ccy = indexByCcy.Filter("GBP");

            var ccyLookup = index.Filter(ccy).ToList();
            var highLookup = index.Filter(high).ToList();
            var lowLookup = index.Filter(low).ToList();
            var result = ccy.Intersect(high).Intersect(low).ToList();
            var lookup = index.Filter(result);

            sw.Stop();
            var elapsed = sw.ElapsedMilliseconds;
            _output.WriteLine($"{elapsed}ms");

            Assert.NotNull(result);
            Assert.NotNull(lookup);
            Assert.True(ccyLookup.All(x => x.Ccy == "GBP"));
            Assert.True(highLookup.All(x => x.High == true));
            Assert.True(lowLookup.All(x => x.Low == true));
            Assert.True(lookup.All(x => x.Ccy == "GBP" && x.High == true && x.Low == true));
        }

        [Fact]
        public void Indexer_Should_Return_Empty_HashSet_When_Lookup_Is_Null()
        {
            var index = new Index<string, TestObject>(x => x.Code);
            var indexer = index.AddIndex("test", x => x.Ccy);

            var result = indexer.Filter(null);

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void Should_Remove_Items_From_Index()
        {
            var index = new Index<string, TestObject>(x => x.Code);

            index.Update(MockData.GenerateTestObjects(20), new List<string> { "Test0", "Test1", "Test2" });

            Assert.Equal(17, index.Count);
        }
    }
}