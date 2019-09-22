using NSubstitute;
using RedisTribute.Io;
using RedisTribute.Io.Commands;
using RedisTribute.Io.Monitoring;
using RedisTribute.Types;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace RedisTribute.UnitTests.Io
{
    public class ConnectionPoolTests
    {
        [Fact]
        public async Task ConnectAsync_SomeConnections_InvokesLeastLoadedWhenExecuteCalled()
        {
            var pool = new ConnectionPool(CreateConnections());
            var cmd = new GetCommand("x");
            var pipe = await pool.RouteCommandAsync(cmd);

            var result = await pipe.Execute(cmd);

            Assert.Equal(10, BitConverter.ToInt32(((RedisString)result).Value));
        }

        [Fact]
        public async Task ConnectAsync_LoadChanged_ReevaluatesWhenExecuteCalled()
        {
            var connections = CreateConnections();
            var pool = new ConnectionPool(connections);
            var cmd = new GetCommand("x");

            var pipe = await pool.RouteCommandAsync(cmd);

            (await connections[4].RouteCommandAsync(cmd)).Metrics.Returns(new PipelineMetrics());

            var pipe2 = await pool.RouteCommandAsync(cmd);

            var result = await pipe.Execute(cmd);
            var result2 = await pipe2.Execute(cmd);

            Assert.Equal(10, BitConverter.ToInt32(((RedisString)result).Value));
            Assert.Equal(5, BitConverter.ToInt32(((RedisString)result2).Value));
        }

        ICommandRouter[] CreateConnections()
        {
            return Enumerable.Range(1, 10).Select(n =>
            {
                var con = Substitute.For<ICommandRouter>();
                var pipelne = Substitute.For<ICommandPipeline>();

                pipelne.Metrics.Returns(new PipelineMetrics(100 - n, 100 - n));

                pipelne
                    .Execute(Arg.Any<IRedisResult<IRedisObject>>(), Arg.Any<CancellationToken>())
                    .Returns(new RedisString(BitConverter.GetBytes(n)));

                con.RouteCommandAsync(Arg.Any<ICommandIdentity>()).Returns(pipelne);
                return con;
            }).ToArray();
        }
    }
}