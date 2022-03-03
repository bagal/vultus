using System;
using System.Collections.Generic;
using System.Linq;

namespace Vultus.Tests.Search
{
    internal static class MockData
    {
        public static Random r = new();
        public static List<string> ccy = new()
        {
            "GBP",
            "EUR",
            "USD"
        };

        public static List<TestObject> GenerateTestObjects(int count)
        {
            return Enumerable.Range(0, count).Select(x => new TestObject { Code = $"Test{x}", Ccy = ccy[N(3)], Balance = N(1000), High = N(2) == 1, Low = N(2) == 1 }).ToList();
        }

        public static int N(int max)
        {
             return r.Next(0, max);
        }
    }
}
