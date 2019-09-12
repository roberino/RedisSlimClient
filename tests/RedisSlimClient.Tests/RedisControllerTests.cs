using NSubstitute;
using RedisSlimClient.Configuration;
using RedisSlimClient.Io;
using RedisSlimClient.Io.Commands;
using RedisSlimClient.Types;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace RedisSlimClient.UnitTests
{
    public class RedisControllerTests
    {
        [Fact]
        public async Task GetResponse_WithProactiveRetryAndFirstResultDelayed_ReturnsSecondResult()
        {
            var connection = Substitute.For<ICommandRouter>();
            var pipeline1 = Substitute.For<ICommandPipeline>();
            var pipeline2 = Substitute.For<ICommandPipeline>();
            var i = 0;

            connection.RouteCommandAsync(Arg.Any<ICommandIdentity>()).Returns(call => (i++ > 0) ? pipeline2 : pipeline1);

            pipeline1.Execute(Arg.Any<GetCommand>(), Arg.Any<CancellationToken>())
                .Returns(async call =>
                {
                    await Task.Run(() => Thread.Sleep(5000));
                    return (IRedisObject)new RedisString(Encoding.ASCII.GetBytes("result1"));
                });

            pipeline2.Execute(Arg.Any<GetCommand>(), Arg.Any<CancellationToken>())
                .Returns(call =>
                {
                    return new RedisString(Encoding.ASCII.GetBytes("result2"));
                });

            var config = new ClientConfiguration("host1")
            {
                FallbackStrategy = FallbackStrategy.ProactiveRetry,
                DefaultOperationTimeout = TimeSpan.FromMinutes(3)
            };

            using (var controller = new RedisController(config, _ => connection))
            {
                var response = await controller.GetResponse(() => new GetCommand("x"), CancellationToken.None, (r, c) => (RedisString)r);

                var resultText = response.ToString();

                Assert.Equal("result2", resultText);
            }
        }
    }
}