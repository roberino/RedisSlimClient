using System;
using System.Linq;

namespace RedisSlimClient.Stubs
{
    public static class ObjectGeneration
    {
        public static TestDtoWithGenericCollection<TestComplexDto> CreateObjectGraph(int numberOfItems = 10)
        {
            return new TestDtoWithGenericCollection<TestComplexDto>()
            {
                Items = Enumerable.Range(1, numberOfItems).Select(n => new TestComplexDto()
                {
                    DataItem1 = n.ToString(),
                    DataItem2 = DateTime.UtcNow,
                    DataItem3 = new TestDtoWithString()
                    {
                        DataItem1 = Guid.NewGuid().ToString()
                    }
                }).ToList()
            };
        }
    }
}