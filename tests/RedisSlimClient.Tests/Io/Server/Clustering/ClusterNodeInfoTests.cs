using RedisSlimClient.Io.Commands;
using RedisSlimClient.Io.Server;
using RedisSlimClient.Io.Server.Clustering;
using Xunit;

namespace RedisSlimClient.UnitTests.Io.Server.Clustering
{
    public class ClusterNodeInfoTests
    {
        [Fact]
        public void CanServe_HashOutsideSlotRange_ReturnsFalse()
        {
            var clusterInf = new ClusterNodeInfo("local", 1234, 4567, null, ServerRoleType.Master,
                new[] {new SlotRange(124, 665)});

            var cmd = new GetCommand("abc"); // 7638

            var canServe = clusterInf.CanServe(cmd);

            Assert.False(canServe);
        }

        [Fact]
        public void CanServe_SlotWithinRange_ReturnsTrue()
        {
            var clusterInf = new ClusterNodeInfo("local", 1234, 4567, null, ServerRoleType.Master,
                new[] { new SlotRange(666, 7639) });

            var cmd = new GetCommand("abc"); // 7638

            var canServe = clusterInf.CanServe(cmd);

            Assert.True(canServe);
        }
    }
}