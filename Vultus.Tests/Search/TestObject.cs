using System;
using System.Collections.Generic;

namespace Vultus.Tests.Search
{
    [Flags]
    internal enum TestStatus
    {
        All = 0,
        Low = 1,
        High = 2
    }

    internal class TestObject
    {
        public string Code { get; set; }
        public string Ccy { get; set; }
        public decimal Balance { get; set; }
        public bool High { get; set; }
        public bool Low { get; set; }
        public TestStatus Status { get; set; }
        public List<string> Ccys { get; set; }
    }
}
