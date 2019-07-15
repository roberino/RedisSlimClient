using RedisSlimClient.Io.Server;
using RedisSlimClient.Io.Server.Clustering;
using RedisSlimClient.Types;
using System.Threading.Tasks;
using Xunit;

namespace RedisSlimClient.UnitTests.Io.Server.Clustering
{
    public class ClusterNodesCommandTests
    {
        [Fact]
        public async Task Complete_ValidResponse_ReturnsClusterNodes()
        {
            var cmd = new ClusterNodesCommand();

            var str = new RedisString("ClusterNodesResponse.csv".OpenBinaryResourceBytes());

            cmd.Complete(str);

            var result = await cmd;

            Assert.Equal(6, result.Count);

            Assert.Equal("127.0.0.1", result[0].Host);
            Assert.Equal(30004, result[0].Port);
            Assert.Equal(ServerRoleType.Slave, result[0].RoleType);
            Assert.Equal(ServerNodeLinkState.Connected, result[0].State);


            Assert.Equal("127.0.0.1", result[1].Host);
            Assert.Equal(30002, result[1].Port);
            Assert.Equal(ServerRoleType.Master, result[1].RoleType);
            Assert.Equal(ServerNodeLinkState.Connected, result[1].State);
            
            Assert.Equal(5461, result[1].Slots[0].Start);
            Assert.Equal(10922, result[1].Slots[0].End);
            Assert.Single(result[1].Slots);
        }
    }
}
