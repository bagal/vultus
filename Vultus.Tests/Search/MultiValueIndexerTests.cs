using System;
using System.Collections.Generic;
using System.Linq;
using Vultus.Tests.Search;
using Xunit;
using Xunit.Abstractions;

namespace Vultus.Search.Tests.Search
{
    public class MultiValueIndexerTests
    {
        ITestOutputHelper _output;

        public MultiValueIndexerTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Should_Filter_Multiple_Values()
        {
            var index = new Index<string, TestObject>(x => x.Code);
            var indexByStatus = index.AddMultiValueIndex("status", x =>
            {
                var result = new List<TestStatus>();

                if ((x.Status & TestStatus.Low) == TestStatus.Low)
                    result.Add(TestStatus.Low);

                if ((x.Status & TestStatus.High) == TestStatus.High)
                    result.Add(TestStatus.High);

                return result;
            });

            var test1 = new TestObject { Code = "Test1", Ccy = "GBP", Balance = 1000, High = true, Low = true, Status = TestStatus.Low | TestStatus.High };
            var test2 = new TestObject { Code = "Test2", Ccy = "EUR", Balance = 1000, High = true, Low = false, Status = TestStatus.High };
            var test3 = new TestObject { Code = "Test3", Ccy = "GBP", Balance = 1000, High = false, Low = true, Status = TestStatus.Low };
            var test4 = new TestObject { Code = "Test4", Ccy = "GBP", Balance = 1000, High = false, Low = true, Status = TestStatus.All };

            index.Update(new List<TestObject> { test1, test2, test3, test4 });

            var result = indexByStatus.Filter(TestStatus.High);
            var lookup = index.Filter(result).ToList();

            Assert.NotNull(lookup);
            Assert.Equal(2, lookup.Count);
        }

        [Fact]
        public void Index_With_Comparer_Should_Filter_Multiple_Values()
        {
            var index = new Index<string, TestObject>(x => x.Code);
            var indexByCcys = index.AddMultiValueIndex("ccys", x => x.Ccys, StringComparer.OrdinalIgnoreCase);

            var test1 = new TestObject { Code = "Test1", Ccy = "GBP", Ccys = new List<string> { "GBP", "USD" }, Balance = 1000, High = true, Low = true, Status = TestStatus.Low | TestStatus.High };
            var test2 = new TestObject { Code = "Test2", Ccy = "EUR", Ccys = new List<string> { "EUR", "USD" }, Balance = 1000, High = true, Low = false, Status = TestStatus.High };
            var test3 = new TestObject { Code = "Test3", Ccy = "GBP", Ccys = new List<string> { "GBP", "USD" }, Balance = 1000, High = false, Low = true, Status = TestStatus.Low };
            var test4 = new TestObject { Code = "Test4", Ccy = "USD", Ccys = new List<string> { "USD" }, Balance = 1000, High = false, Low = true, Status = TestStatus.All };

            index.Update(new List<TestObject> { test1, test2, test3, test4 });

            var result = indexByCcys.Filter("UsD");
            var lookup = index.Filter(result).ToList();

            Assert.NotNull(lookup);
            Assert.Equal(4, lookup.Count);
        }
    }
}
