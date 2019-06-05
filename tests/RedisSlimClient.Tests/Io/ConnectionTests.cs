using NSubstitute;
using RedisSlimClient.Io;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace RedisSlimClient.UnitTests.Io
{
    public class ConnectionTests
    {
        readonly ITestOutputHelper _output;
        readonly ICommandPipeline _pipeline;
        readonly INetworkStreamFactory _streamFactory;

        public ConnectionTests(ITestOutputHelper output)
        {
            _output = output;
            _pipeline = Substitute.For<ICommandPipeline>();
            _streamFactory = Substitute.For<INetworkStreamFactory>();
        }

        [Fact]
        public async Task ConnectAsync_ReturnsPipelineFromFactory()
        {
            using (var connection = new Connection(_streamFactory, null, s => Task.FromResult(_pipeline)))
            {
                var pipeline = await connection.ConnectAsync();

                Assert.Same(pipeline, _pipeline);
            }
        }

        [Fact]
        public async Task Dispose_CallsDisposeOnHeldResources()
        {
            using (var connection = new Connection(_streamFactory, null, s => Task.FromResult(_pipeline)))
            {
                await connection.ConnectAsync();
            }

            _pipeline.Received().Dispose();
            _streamFactory.Received().Dispose();
        }

        [Fact]
        public void WorkLoad_BeforeConnect_ReturnsZero()
        {
            _pipeline.PendingWork.Returns((7, 13));

            using (var connection = new Connection(_streamFactory, null, s => Task.FromResult(_pipeline)))
            {
                var load = connection.WorkLoad;

                Assert.Equal(0, load);
            }
        }

        [Fact]
        public async Task WorkLoad_AfterConnect_ReturnsPendingReadsAndWritesMultiplied()
        {
            _pipeline.PendingWork.Returns((7, 13));

            using (var connection = new Connection(_streamFactory, null, s => Task.FromResult(_pipeline)))
            {
                await connection.ConnectAsync();

                var load = connection.WorkLoad;

                Assert.Equal(7 * 13, load);
            }
        }
    }
}