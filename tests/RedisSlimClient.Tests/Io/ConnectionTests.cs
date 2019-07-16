//using NSubstitute;
//using RedisSlimClient.Io;
//using RedisSlimClient.Io.Commands;
//using RedisSlimClient.Io.Server;
//using System.Threading.Tasks;
//using Xunit;
//using Xunit.Abstractions;

//namespace RedisSlimClient.UnitTests.Io
//{
//    public class ConnectionTests
//    {
//        readonly ITestOutputHelper _output;
//        readonly ICommandPipeline _pipeline;
//        readonly IServerNodeInitialiser _pipelineInitialiser;
//        readonly IConnectedPipeline _connectedPipeline;

//        public ConnectionTests(ITestOutputHelper output)
//        {
//            _output = output;
//            _pipeline = Substitute.For<ICommandPipeline>();
//            _pipelineInitialiser = Substitute.For<IServerNodeInitialiser>();
//            _connectedPipeline = Substitute.For<IConnectedPipeline>();
//            _connectedPipeline.GetPipeline().Returns(_pipeline);

//            _pipelineInitialiser.InitialiseAsync().Returns(new[] { _connectedPipeline });
//        }

//        Connection CreateConnection()
//        {
//            return new Connection(_pipelineInitialiser);
//        }

//        [Fact]
//        public async Task ConnectAsync_ReturnsPipelineFromFactory()
//        {
//            using (var connection = CreateConnection())
//            {
//                var pipeline = await connection.RouteCommandAsync(Substitute.For<ICommandIdentity>());

//                Assert.Same(pipeline, _pipeline);
//            }
//        }

//        [Fact]
//        public async Task Dispose_CallsDisposeOnPipeline()
//        {
//            using (var connection = CreateConnection())
//            {
//                await connection.RouteCommandAsync(Substitute.For<ICommandIdentity>());
//            }

//            _connectedPipeline.Received().Dispose();
//        }

//        [Fact]
//        public void WorkLoad_BeforeConnect_ReturnsZero()
//        {
//            _pipeline.PendingWork.Returns((7, 13));

//            using (var connection = CreateConnection())
//            {
//                var load = connection.WorkLoad;

//                Assert.Equal(0, load);
//            }
//        }

//        [Fact]
//        public async Task WorkLoad_AfterConnect_ReturnsPendingReadsAndWritesMultiplied()
//        {
//            _pipeline.PendingWork.Returns((7, 13));

//            using (var connection = CreateConnection())
//            {
//                await connection.RouteCommandAsync(Substitute.For<ICommandIdentity>());

//                var load = connection.WorkLoad;

//                Assert.Equal(7 * 13, load);
//            }
//        }
//    }
//}