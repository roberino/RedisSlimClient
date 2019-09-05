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

        public PortMap Import(string portMappings)
        {
            foreach(var pair in portMappings.Split(',').Select(p => p.Split(':')).Select(a => (from : int.Parse(a[0]), to : int.Parse(a[1]))))
            {
                _map[pair.from] = pair.to;
            }

            return this;
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

        public PortMap Clone() => new PortMap().Import(ToString());

        public IEnumerator<(int From, int To)> GetEnumerator() => _map.Select(m => (m.Key, m.Value)).GetEnumerator();

        public override string ToString() => string.Join(",", _map.Select(m => $"{m.Key}:{m.Value}"));

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}