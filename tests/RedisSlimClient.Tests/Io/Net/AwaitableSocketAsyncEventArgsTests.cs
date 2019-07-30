using RedisSlimClient.Io.Net;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace RedisSlimClient.UnitTests.Io.Net
{
    public class AwaitableSocketAsyncEventArgsTests
    {
        [Fact]
        public async Task Complete_ThenAwaitWithContinuation_ReturnsResult()
        {
            var tasks = Enumerable.Range(1, 100)
                .Select(async n =>
                {
                    using (var args = new AwaitableSocketAsyncEventArgs())
                    {
                        args.Reset(new ReadOnlyMemory<byte>(new byte[8]));

                        ThreadPool.QueueUserWorkItem(_ => args.Complete(), null);

                        var result = await args;

                        result++;

                        return result;
                    }
                });

            var results = await Task.WhenAll(tasks);

            Assert.Equal(100, results.Length);
            Assert.Equal(100, results.Sum());
        }
    }
}
