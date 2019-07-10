using RedisSlimClient.Io.Commands;
using RedisSlimClient.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RedisSlimClient.Io.Clustering
{
    class ClusterNodesCommand : RedisCommand<ClusterSlotsConfiguration>
    {
        public ClusterNodesCommand() : base("CLUSTER NODES") { }

        protected override ClusterSlotsConfiguration TranslateResult(IRedisObject redisObject)
        {
            var reader = new StringReader(redisObject.ToString());
            var config = new ClusterSlotsConfiguration();

            //  <id> <ip:port> <flags> <master> <ping-sent> <pong-recv> <config-epoch> <link-state> <slot> <slot> ... <slot>

            while (true)
            {
                var next = reader.ReadLine();

                if (next == null)
                {
                    break;
                }

                var parts = next.Split(' ');

                var id = parts[0];
                var ipPort = parts[1].Split(':');
                var flags = parts[2].Split(',');
                var masterNode = parts[3];
                var state = ParseLinkState(parts[7]);
                var slots = new List<SlotRange>();
                var role = ParseServerRole(flags);

                if (parts.Length > 8)
                {
                    for (var i = 8; i < parts.Length; i++)
                    {
                        var slotRanges = parts[i].Split('-').Select(s => long.Parse(s)).ToArray();
                        slots.Add(new SlotRange(slotRanges[0], slotRanges[1]));
                    }
                }

                config.Add(new ClusterNode(id, flags, masterNode, role, state,
                    new ClusterInfo(ipPort[0], int.Parse(ipPort[1]), slots.ToArray())));
            }

            return config;
        }

        ServerNodeLinkState ParseLinkState(string value)
        {
            if (Enum.TryParse<ServerNodeLinkState>(value, true, out var state))
            {
                return state;
            }

            return ServerNodeLinkState.Unknown;
        }

        ServerRoleType ParseServerRole(string[] flags)
        {
            foreach(var flag in flags)
            {
                if (Enum.TryParse<ServerRoleType>(flag, true, out var nodeType))
                {
                    return nodeType;
                }
            }

            return ServerRoleType.Unknown;
        }
    }
}