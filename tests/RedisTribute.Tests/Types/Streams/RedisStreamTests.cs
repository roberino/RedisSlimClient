using NSubstitute;
using RedisTribute.Configuration;
using RedisTribute.Types;
using RedisTribute.Types.Streams;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace RedisTribute.UnitTests.Types.Streams
{
    public class RedisStreamTests
    {
        [Fact]
        public async Task WriteAsync_SomeData_SendsSerializedKeyValues()
        {
            var cancellation = new CancellationTokenSource();
            var client = Substitute.For<IPrimativeStreamClient>();
            var stream = new RedisStream<string>(client, new ClientConfiguration("localhost"), "xyz");

            await stream.WriteAsync("test", cancellation.Token);

            await client.Received().XAddAsync(Arg.Is<RedisKey>(k => k.Equals("xyz")), Arg.Is<IDictionary<RedisKey, RedisKey>>(x => x.Count == 1 && x["$data"].ToString() == "test"), cancellation.Token);
        }
    }
}
