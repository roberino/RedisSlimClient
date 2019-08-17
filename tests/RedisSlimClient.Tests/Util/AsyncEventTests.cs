using RedisSlimClient.Util;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace RedisSlimClient.UnitTests.Util
{
    public class AsyncEventTests
    {
        [Fact]
        public async Task PublishAsync_SomeEvent_IsReceivedBySubscriber()
        {
            var received = new List<string>();
            var ev = new AsyncEvent<string>();

            ev.Subscribe(x =>
            {
                received.Add(x);
                return Task.CompletedTask;
            });

            await ev.PublishAsync("s1");

            Assert.Single(received);

            await ev.PublishAsync("s2");

            Assert.Equal("s1", received[0]);
            Assert.Equal("s2", received[1]);
        }
    }
}