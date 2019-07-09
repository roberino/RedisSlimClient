using RedisSlimClient.Io.Commands;
using RedisSlimClient.Types;
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
                var state = parts[7];
                var slots = new List<SlotRange>();

                if (parts.Length > 8)
                {
                    for (var i = 8; i < parts.Length; i++)
                    {
                        var slotRanges = parts[i].Split('-').Select(s => long.Parse(s)).ToArray();
                        slots.Add(new SlotRange(slotRanges[0], slotRanges[1]));
                    }
                }
            }

            return config;
        }
    }
}