namespace RedisSlimClient.Io.Clustering
{
    class ClusterNode : ClusterInfo
    {
        public ClusterNode(string id, string[] flags, string masterNodeId, ServerRoleType nodeType, ServerNodeLinkState state, ClusterInfo clusterInfo)
            : base(clusterInfo.Host, clusterInfo.Port, clusterInfo.Slots)
        {
            Id = id;
            Flags = flags;
            RoleType = nodeType;
            State = state;
            MasterNodeId = masterNodeId;
        }


        // <id> <ip:port> <flags> <master> <ping-sent> <pong-recv> <config-epoch> <link-state> <slot> <slot> ... <slot>

        public string Id { get; }

        public string[] Flags { get; }

        public ServerRoleType RoleType { get; }

        public string MasterNodeId { get; }

        public ServerNodeLinkState State { get; }
    }
}