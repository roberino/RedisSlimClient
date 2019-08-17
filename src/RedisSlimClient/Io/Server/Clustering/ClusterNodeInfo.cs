﻿using RedisSlimClient.Configuration;
using RedisSlimClient.Io.Commands;
using RedisSlimClient.Types;
using System.Linq;

namespace RedisSlimClient.Io.Server.Clustering
{
    class ClusterNodeInfo : ServerEndPointInfo
    {
        public ClusterNodeInfo(string host, int port, int mappedPort, IHostAddressResolver dnsResolver, ServerRoleType role, SlotRange[] slots) : base(host, port, mappedPort, dnsResolver, role)
        {
            Slots = slots;
        }

        public override bool IsCluster => true;

        public SlotRange[] Slots { get; }

        public override bool CanServe(ICommandIdentity command, RedisKey key = default)
        {
            if (key.IsNull) key = command.Key;

            return base.CanServe(command) && (key.IsNull || Slots.Any(s => s.IsWithinRange(HashGenerator.Generate(key.Bytes))));
        }
    }
}