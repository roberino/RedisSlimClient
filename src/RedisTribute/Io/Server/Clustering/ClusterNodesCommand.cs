using RedisTribute.Configuration;
using RedisTribute.Io.Commands;
using RedisTribute.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RedisTribute.Io.Server.Clustering
{
    class ClusterNodesCommand : RedisCommand<IList<ClusterNode>>
    {
        readonly object[] _args;
        readonly NetworkConfiguration _networkConfiguration;

        public ClusterNodesCommand(NetworkConfiguration networkConfiguration) : base("CLUSTER NODES")
        {
            _networkConfiguration = networkConfiguration;
            _args = CommandText.Split(' ');
        }

        protected override CommandParameters GetArgs() => _args;

        protected override IList<ClusterNode> TranslateResult(IRedisObject redisObject)
        {
            var reader = new StringReader(redisObject.ToString()!);
            var config = new List<ClusterNode>();

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
                        var slotRanges = parts[i].Split('-').Select(ParseSlot).ToArray();
                        slots.Add(new SlotRange(slotRanges[0], slotRanges.Length > 1 ? slotRanges[1] : slotRanges[0]));
                    }
                }

                var port = int.Parse(ipPort[1].Split('@')[0]);
                var mappedPort = _networkConfiguration.PortMappings.Map(port);

                config.Add(new ClusterNode(id, flags, masterNode, state,
                    new ClusterNodeInfo(ipPort[0], port, mappedPort, _networkConfiguration.DnsResolver, role, slots.ToArray())));
            }

            return config;
        }

        long ParseSlot(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return 0;
            }

            if (value[0] == '>' || value[0] == '<')
            {
                return 0;
            }

            long.TryParse(value, out var r);

            return r;
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
            foreach (var flag in flags)
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