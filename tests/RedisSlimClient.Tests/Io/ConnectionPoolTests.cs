using NSubstitute;
using RedisSlimClient.Io;
using RedisSlimClient.Io.Commands;
using RedisSlimClient.Types;
using System;
using System.Linq;
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

            var pipe = await pool.ConnectAsync();

            var result = await pipe.Execute(new GetCommand("x"), TimeSpan.FromMilliseconds(10));

            Assert.Equal(10, BitConverter.ToInt32(((RedisString)result).Value));
        }

        [Fact]
        public async Task ConnectAsync_LoadChanged_ReevaluatesWhenExecuteCalled()
        {
            var connections = CreateConnections();
            var pool = new ConnectionPool(connections);

            var pipe = await pool.ConnectAsync();

            connections[4].WorkLoad.Returns(-1f);

            var pipe2 = await pool.ConnectAsync();

            var result = await pipe.Execute(new GetCommand("x"), TimeSpan.FromMilliseconds(10));
            var result2 = await pipe2.Execute(new GetCommand("x"), TimeSpan.FromMilliseconds(10));

            Assert.Equal(10, BitConverter.ToInt32(((RedisString)result).Value));
            Assert.Equal(5, BitConverter.ToInt32(((RedisString)result2).Value));
        }

        IConnection[] CreateConnections()
        {
            return Enumerable.Range(1, 10).Select(n =>
            {
                var con = Substitute.For<IConnection>();
                var pipelne = Substitute.For<ICommandPipeline>();

                pipelne.Execute(Arg.Any<IRedisResult<RedisObject>>(), Arg.Any<TimeSpan>())
                .Returns(new RedisString(BitConverter.GetBytes(n)));

                con.ConnectAsync().Returns(pipelne);
                con.Id.Returns(n.ToString());
                con.WorkLoad.Returns(1f / n);
                return con;
            }).ToArray();
        }
    }
}