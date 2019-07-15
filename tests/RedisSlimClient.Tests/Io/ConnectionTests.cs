using NSubstitute;
using RedisSlimClient.Io;
using RedisSlimClient.Io.Commands;
using RedisSlimClient.Io.Server;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace RedisSlimClient.UnitTests.Io
{
    public class ConnectionTests
    {
        readonly ITestOutputHelper _output;
        readonly ICommandPipeline _pipeline;
        readonly IServerNodeInitialiser _pipelineInitialiser;

        public ConnectionTests(ITestOutputHelper output)
        {
            _output = output;
            _pipeline = Substitute.For<ICommandPipeline>();
            _pipelineInitialiser = Substitute.For<IServerNodeInitialiser>();
        }

        Connection CreateConnection()
        {
            return new Connection(new ServerEndPointInfo("a", 123), _ => Task.FromResult(_pipeline), _pipelineInitialiser);
        }

        [Fact]
        public async Task ConnectAsync_ReturnsPipelineFromFactory()
        {
            using (var connection = CreateConnection())
            {
                var pipeline = await connection.RouteCommandAsync(Substitute.For<ICommandIdentity>());

                Assert.Same(pipeline, _pipeline);
            }
        }

        [Fact]
        public async Task Dispose_CallsDisposeOnPipeline()
        {
            using (var connection = CreateConnection())
            {
                await connection.RouteCommandAsync(Substitute.For<ICommandIdentity>());
            }

            _pipeline.Received().Dispose();
        }

        [Fact]
        public void WorkLoad_BeforeConnect_ReturnsZero()
        {
            _pipeline.PendingWork.Returns((7, 13));

            using (var connection = CreateConnection())
            {
                var load = connection.WorkLoad;

                Assert.Equal(0, load);
            }
        }

        [Fact]
        public async Task WorkLoad_AfterConnect_ReturnsPendingReadsAndWritesMultiplied()
        {
            _pipeline.PendingWork.Returns((7, 13));

            using (var connection = CreateConnection())
            {
                await connection.RouteCommandAsync(Substitute.For<ICommandIdentity>());

                var load = connection.WorkLoad;

                Assert.Equal(7 * 13, load);
            }
        }
    }
}