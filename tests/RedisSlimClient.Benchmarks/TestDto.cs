using System;
using System.Collections.Generic;

namespace RedisSlimClient.Benchmarks
{
    public class TestComplexDto
    {
        public string DataItem1 { get; set; }

        public DateTime DataItem2 { get; set; }

        public TestDtoWithString DataItem3 { get; set; }
    }

    public class TestDtoWithGenericCollection<T>
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public IList<T> Items { get; set; } = new List<T>();
    }

    public class TestDtoWithCollection
    {
        public TestDtoWithString[] DataItems { get; set; }
    }

    public class TestDtoWithInt
    {
        public int DataItem1 { get; set; }
    }

    public class TestDtoWithString
    {
        public string DataItem1 { get; set; }
    }

    public class TestDtoWithGeneric<T>
    {
        public T DataItem1 { get; set; }
    }
}
