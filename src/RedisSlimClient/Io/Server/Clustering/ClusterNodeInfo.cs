using RedisSlimClient.Configuration;
using RedisSlimClient.Io.Commands;
using System.Linq;

namespace RedisSlimClient.Io.Server.Clustering
{
    class ClusterNodeInfo : ServerEndPointInfo
    {
        public ClusterNodeInfo(string host, int port, int mappedPort, IHostAddressResolver dnsResolver, ServerRoleType role, SlotRange[] slots) : base(host, port, mappedPort, dnsResolver, role)
        {
            Slots = slots;
        }

        public SlotRange[] Slots { get; }

        public override bool CanServe(ICommandIdentity command)
        {
            return base.CanServe(command) && (command.Key.IsNull || Slots.Any(s => s.IsWithinRange(HashGenerator.Generate(command.Key.Bytes))));
        }
    }
}