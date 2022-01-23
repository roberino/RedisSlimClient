using RedisTribute.Util;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace RedisTribute.UnitTests.Util
{
    public class SyncronizedInstanceTests
    {
        [Fact]
        public async Task GetValue_SingleCall_ReturnsValue()
        {
            var instance = new SyncronizedInstance<string>(async () => await Task.Delay(10).ContinueWith(_ => Guid.NewGuid().ToString()));

            var result = await instance.GetValue();

            Assert.NotEmpty(result.ToString());
        }

        [Fact]
        public void GetValue_MultipleCallsCrossThread_ReturnsValue()
        {
            var instance = new SyncronizedInstance<string>(async () => await Task.Delay(10).ContinueWith(_ => Guid.NewGuid().ToString()));

            var results = new ConcurrentBag<string>();

            Enumerable.Range(1, 1000).AsParallel().ForAll(n =>
            {
                results.Add(instance.GetValue().GetAwaiter().GetResult().ToString());
            });

            Assert.Single(results.Distinct());
        }
    }
}
