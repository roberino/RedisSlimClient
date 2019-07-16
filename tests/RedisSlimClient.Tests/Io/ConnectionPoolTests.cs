using NSubstitute;
using RedisSlimClient.Io;
using RedisSlimClient.Io.Commands;
using RedisSlimClient.Types;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace RedisSlimClient.UnitTests.Io
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

            connections[4].CalculateWorkLoad(cmd).Returns(-1f);

            var pipe2 = await pool.RouteCommandAsync(cmd);

            var result = await pipe.Execute(cmd);
            var result2 = await pipe2.Execute(cmd);

            Assert.Equal(10, BitConverter.ToInt32(((RedisString)result).Value));
            Assert.Equal(5, BitConverter.ToInt32(((RedisString)result2).Value));
        }

        IConnection[] CreateConnections()
        {
            return Enumerable.Range(1, 10).Select(n =>
            {
                var con = Substitute.For<IConnection>();
                var pipelne = Substitute.For<ICommandPipeline>();

                pipelne
                    .Execute(Arg.Any<IRedisResult<IRedisObject>>(), Arg.Any<CancellationToken>())
                    .Returns(new RedisString(BitConverter.GetBytes(n)));

                con.RouteCommandAsync(Arg.Any<ICommandIdentity>()).Returns(pipelne);
                con.Id.Returns(n.ToString());
                con.CalculateWorkLoad(Arg.Any<ICommandIdentity>()).Returns(1f / n);
                return con;
            }).ToArray();
        }
    }
}