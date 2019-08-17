using System.Collections.Generic;

namespace RedisSlimClient.Io.Server
{
    readonly struct ServerRole
    {
        public ServerRole(ServerRoleType roleType, ServerEndPointInfo  master, IReadOnlyCollection<ServerEndPointInfo> slaves)
        {
            RoleType = roleType;
            Master = master;
            Slaves = slaves;
        }

        public ServerRoleType RoleType { get; }
        public ServerEndPointInfo Master { get; }
        public IReadOnlyCollection<ServerEndPointInfo> Slaves { get; }
    }
}