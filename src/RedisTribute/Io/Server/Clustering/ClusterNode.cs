﻿using System;
using System.Linq;

namespace RedisTribute.Io.Server.Clustering
{
    class ClusterNode : ClusterNodeInfo
    {
        public ClusterNode(string id, string[] flags, string masterNodeId, ServerNodeLinkState state, ClusterNodeInfo clusterInfo)
            : base(clusterInfo.Host, clusterInfo.Port, clusterInfo.MappedPort, clusterInfo.DnsResolver, clusterInfo.RoleType, clusterInfo.Slots)
        {
            Id = id;
            Flags = flags;
            State = state;
            MasterNodeId = masterNodeId;
        }

        // <id> <ip:port> <flags> <master> <ping-sent> <pong-recv> <config-epoch> <link-state> <slot> <slot> ... <slot>

        public string Id { get; }

        public bool IsMyself => Flags.Contains("myself", StringComparer.OrdinalIgnoreCase);

        public string[] Flags { get; }

        public string MasterNodeId { get; }

        public ServerNodeLinkState State { get; }
    }
}