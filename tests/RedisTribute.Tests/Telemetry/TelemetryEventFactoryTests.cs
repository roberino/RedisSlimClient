using RedisTribute.Telemetry;
using System;
using System.Collections.Concurrent;
using System.Linq;
using Xunit;

namespace RedisTribute.UnitTests.Telemetry
{
    public class TelemetryEventFactoryTests
    {
        [Fact]
        public void Create_SizeCorrectBeforeAndAfterItem()
        {
            var pool = new TelemetryEventFactory(4);

            Assert.Equal(4, pool.Size);

            var item = pool.Create("x");

            Assert.Equal(4, pool.Size);
            Assert.Equal(3, pool.Available);

            item.Dispose();

            Assert.Equal(4, pool.Available);
        }

        [Fact]
        public void Create_ReusedInstance_PropertiesAreReset()
        {
            var pool = new TelemetryEventFactory(4);

            Assert.Equal(4, pool.Size);

            var items = Enumerable.Range(1, 4).Select(n => pool.Create(n.ToString())).ToList();

            var ids = items.Select(x => x.OperationId).ToList();
            
            foreach (var item in items)
            {
                item.Dispose();
            }

            var reusedItem = pool.Create("x");

            Assert.Equal("x", reusedItem.Name);
            Assert.Equal(0, reusedItem.Dimensions.Count);
            Assert.Equal(TimeSpan.Zero, reusedItem.Elapsed);
            Assert.DoesNotContain(reusedItem.OperationId, ids);
        }

        [Fact]
        public void Create_ReusedInstancesAcrossThreads_UniqueIdsGenerated()
        {
            var pool = new TelemetryEventFactory(4);
            var capturedIds = new ConcurrentBag<string>();

            Enumerable.Range(1, 50)
                .AsParallel().ForAll(n =>
                {
                    using (var t = pool.Create(n.ToString()))
                    {
                        capturedIds.Add(t.OperationId);
                        Assert.Equal(n.ToString(), t.Name);
                    }
                });

            Assert.Equal(50, capturedIds.Distinct().Count());
        }

        [Theory]
        [InlineData(4, 1)]
        [InlineData(2, 1)]
        [InlineData(10, 2)]
        [InlineData(14, 3)]
        public void Create_ExceedPoolSize_PoolIsExpanded(int minSize, int expectedGrowRate)
        {
            var pool = new TelemetryEventFactory(minSize);

            Assert.Equal(minSize, pool.Size);

            var items = Enumerable.Range(1, minSize + 1).Select(n => pool.Create(n.ToString())).ToList();

            Assert.Equal(minSize + expectedGrowRate, pool.Size);
            Assert.Equal(expectedGrowRate - 1, pool.Available);

            var i = 1;

            foreach(var item in items)
            {
                Assert.Equal((i++).ToString(), item.Name);
                item.Dispose();
            }

            Assert.Equal(minSize + expectedGrowRate, pool.Available);
        }


        [Fact]
        public void Create_ExceedMaxSize_ThrowsException()
        {
            var pool = new TelemetryEventFactory(4, 8);

            Assert.Throws<InvalidOperationException>(() => Enumerable.Range(1, 9).Select(n => pool.Create(n.ToString())).ToList());
        }
    }
}
