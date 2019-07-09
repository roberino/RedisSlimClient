namespace RedisSlimClient.Io.Clustering
{
    class ClusterNode : ClusterInfo
    {
        public ClusterNode(string id, string[] flags, string masterNodeId, ServerNodeType nodeType, string state, ClusterInfo clusterInfo)
            : base(clusterInfo.Host, clusterInfo.Port, clusterInfo.Slots)
        {
            Id = id;
            Flags = flags;
            NodeType = nodeType;
            State = state;
            MasterNodeId = masterNodeId;
        }


        // <id> <ip:port> <flags> <master> <ping-sent> <pong-recv> <config-epoch> <link-state> <slot> <slot> ... <slot>

        public string Id { get; }

        public string[] Flags { get; }

        public ServerNodeType NodeType { get; }

        public string MasterNodeId { get; }

        public string State { get; }
    }
}