using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace RedisSlimClient.Configuration
{
    public sealed class PortMap : IEnumerable<(int From, int To)>
    {
        readonly IDictionary<int, int> _map;

        public PortMap(IEnumerable<(int from, int to)> portMappings)
        {
            _map = portMappings.ToDictionary(m => m.from, m => m.to);
        }

        public PortMap()
        {
            _map = new Dictionary<int, int>();
        }

        public void Import(string portMappings)
        {
            foreach(var pair in portMappings.Split(',').Select(p => p.Split(':')).Select(a => (from : int.Parse(a[0]), to : int.Parse(a[1]))))
            {
                _map[pair.from] = pair.to;
            }
        }

        public PortMap Map(int fromPort, int toPort)
        {
            _map[fromPort] = toPort;

            return this;
        }

        public int Map(int fromPort)
        {
            if (_map.TryGetValue(fromPort, out var toPort))
            {
                return toPort;
            }

            return fromPort;
        }

        public IEnumerator<(int From, int To)> GetEnumerator() => _map.Select(m => (m.Key, m.Value)).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}