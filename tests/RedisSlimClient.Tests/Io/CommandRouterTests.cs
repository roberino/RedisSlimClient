using NSubstitute;
using RedisSlimClient.Io;
using RedisSlimClient.Io.Commands;
using RedisSlimClient.Io.Server;
using RedisSlimClient.Types;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace RedisSlimClient.UnitTests.Io
{
    public class CommandRouterTests
    {
        readonly ITestOutputHelper _output;
        readonly (IConnectionSubordinate sub, ICommandPipeline pip) _sub1;
        readonly (IConnectionSubordinate sub, ICommandPipeline pip) _sub2;
        readonly (IConnectionSubordinate sub, ICommandPipeline pip) _subCluster1;
        readonly (IConnectionSubordinate sub, ICommandPipeline pip) _subCluster2;
        readonly IServerNodeInitialiser _pipelineInitialiser;
        readonly IServerNodeInitialiser _pipelineInitialiserCluster;

        public CommandRouterTests(ITestOutputHelper output)
        {
            _output = output;
            _pipelineInitialiser = Substitute.For<IServerNodeInitialiser>();
            _pipelineInitialiserCluster = Substitute.For<IServerNodeInitialiser>();

            _sub1 = CreateConnectionSubordinate(1);
            _sub2 = CreateConnectionSubordinate(2);
            _subCluster1 = CreateConnectionSubordinate(3, true);
            _subCluster2 = CreateConnectionSubordinate(4, true);

            _pipelineInitialiser.InitialiseAsync().Returns(new[] { _sub1.sub, _sub2.sub });
            _pipelineInitialiserCluster.InitialiseAsync().Returns(new[] { _subCluster1.sub, _subCluster2.sub });
        }

        static (IConnectionSubordinate sub, ICommandPipeline pip) CreateConnectionSubordinate(int id, bool isCluster = false)
        {
            var pipeline = Substitute.For<ICommandPipeline>();
            var connectedSubordinate = Substitute.For<IConnectionSubordinate>();
            connectedSubordinate.GetPipeline().Returns(pipeline);
            connectedSubordinate.EndPointInfo.Returns(new TestServerInfo(id, isCluster));
            connectedSubordinate.Status.Returns(PipelineStatus.Ok);
            return (connectedSubordinate, pipeline);
        }

        CommandRouter CreateConnection()
        {
            return new CommandRouter(_pipelineInitialiser);
        }

        CommandRouter CreateClusteredConnection()
        {
            return new CommandRouter(_pipelineInitialiserCluster);
        }

        [Fact]
        public async Task RouteCommandAsync_ReturnsFirstPipeline()
        {
            using (var connection = CreateConnection())
            {
                var pipeline = await connection.RouteCommandAsync(Substitute.For<ICommandIdentity>());

                Assert.Same(pipeline, _sub1.pip);
            }
        }

        [Fact]
        public async Task RouteCommandAsync_FirstConnectionBroken_ReturnsSecond()
        {
            using (var connection = CreateConnection())
            {
                WhenFirstConnectionIsBroken();

                var pipeline = await connection.RouteCommandAsync(Substitute.For<ICommandIdentity>());

                Assert.Same(pipeline, _sub2.pip);
            }
        }

        [Fact]
        public async Task RouteCommandAsync_FirstConnectionNotMatch_ReturnsSecond()
        {
            using (var connection = CreateConnection())
            {
                WhenFirstConnectionIsNonMatch();

                var pipeline = await connection.RouteCommandAsync(Substitute.For<ICommandIdentity>());

                Assert.Same(pipeline, _sub2.pip);
            }
        }

        [Fact]
        public async Task RouteCommandAsync_AllNodes_ReturnsBothConnections()
        {
            using (var connection = CreateConnection())
            {
                var pipelines = await connection.RouteCommandAsync(Substitute.For<ICommandIdentity>(), ConnectionTarget.AllNodes);

                Assert.Equal(2, pipelines.Count());
                Assert.Equal(_sub1.pip, pipelines.ElementAt(0));
                Assert.Equal(_sub2.pip, pipelines.ElementAt(1));
            }
        }

        [Fact]
        public async Task RouteCommandAsync_AllAvailableMasters_ReturnsMasterConnection()
        {
            using (var connection = CreateConnection())
            {
                _sub1.sub.EndPointInfo.UpdateRole(ServerRoleType.Slave);

                var pipelines = await connection.RouteCommandAsync(Substitute.For<ICommandIdentity>(), ConnectionTarget.AllAvailableMasters);

                Assert.Single(pipelines);
                Assert.Equal(_sub2.pip, pipelines.ElementAt(0));
            }
        }

        [Fact]
        public async Task RouteMultiKeyCommandAsync_ClusteredConnection_ReturnsConnectionsSplitByHash()
        {
            using (var connection = CreateClusteredConnection())
            {
                var key1 = (RedisKey)"a";
                var key2 = (RedisKey)"b";

                WhenConnectionMatchesKey(_subCluster1.sub, key1);
                WhenConnectionMatchesKey(_subCluster2.sub, key2);

                var cmd = Substitute.For<IMultiKeyCommandIdentity>();

                cmd.Keys.Returns(new[] { key1, key2 });

                var routes = (await connection.RouteMultiKeyCommandAsync(cmd)).ToArray();

                Assert.Equal(key1, routes[0].Keys.Single());
                Assert.Equal(_subCluster1.pip, routes[0].Executor);
                Assert.Equal(key2, routes[1].Keys.Single());
                Assert.Equal(_subCluster2.pip, routes[1].Executor);
            }
        }

        [Fact]
        public async Task RouteMultiKeyCommandAsync_NonClusteredConnection_ReturnsSingleRoute()
        {
            using (var connection = CreateConnection())
            {
                var key1 = (RedisKey)"a";
                var key2 = (RedisKey)"b";

                var cmd = Substitute.For<IMultiKeyCommandIdentity>();

                cmd.Keys.Returns(new[] { key1, key2 });

                var routes = (await connection.RouteMultiKeyCommandAsync(cmd)).ToArray();

                Assert.Single(routes);
                Assert.Equal(key1, routes[0].Keys.ElementAt(0));
                Assert.Equal(key2, routes[0].Keys.ElementAt(1));
            }
        }

        [Fact]
        public async Task RouteCommandAsync_AllConnectionsBroken_ReturnsBrokenConnection()
        {
            using (var connection = CreateConnection())
            {
                WhenAllConnectionsBroken();

                var selected = await connection.RouteCommandAsync(Substitute.For<ICommandIdentity>());

                Assert.NotNull(selected);
            }
        }

        [Fact]
        public async Task Dispose_CallsDisposeOnPipeline()
        {
            using (var connection = CreateConnection())
            {
                await connection.RouteCommandAsync(Substitute.For<ICommandIdentity>());
            }

            _sub1.sub.Received().Dispose();
        }

        void WhenFirstConnectionIsBroken()
        {
            _sub1.sub.Status.Returns(PipelineStatus.Broken);
        }

        void WhenAllConnectionsBroken()
        {
            _sub1.sub.Status.Returns(PipelineStatus.Broken);
            _sub2.sub.Status.Returns(PipelineStatus.Broken);
        }

        void WhenFirstConnectionIsNonMatch()
        {
            ((TestServerInfo)_sub1.sub.EndPointInfo).Enabled = false;
        }

        void WhenConnectionMatchesKey(IConnectionSubordinate sub, RedisKey key)
        {
            ((TestServerInfo)sub.EndPointInfo).KeyMatch = key;
        }

        class TestServerInfo : ServerEndPointInfo
        {
            public TestServerInfo(int id, bool isCluster = false) : base($"localhost{id}", 1234, 2345, null, ServerRoleType.Master)
            {
                IsCluster = isCluster;
            }

            public bool Enabled { get; set; } = true;

            public override bool IsCluster { get; }

            public RedisKey KeyMatch { get; set; }

            public override bool CanServe(ICommandIdentity command, RedisKey key = default)
            {
                return Enabled && (KeyMatch.IsNull || KeyMatch.Equals(key));
            }
        }
    }
}