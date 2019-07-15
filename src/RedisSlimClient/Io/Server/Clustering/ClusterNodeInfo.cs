using RedisSlimClient.Io.Commands;
using System.Linq;

namespace RedisSlimClient.Io.Server.Clustering
{
    class ClusterNodeInfo : ServerEndPointInfo
    {
        public ClusterNodeInfo(string host, int port, ServerRoleType role, SlotRange[] slots) : base(host, port, role)
        {
            Slots = slots;
        }

        public SlotRange[] Slots { get; }

        public override bool CanServe(ICommandIdentity command)
        {
            return base.CanServe(command) && Slots.Any(s => s.IsWithinRange(HashGenerator.Generate(command.Key)));
        }
    }
}