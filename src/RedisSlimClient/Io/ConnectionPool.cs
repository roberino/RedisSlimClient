using RedisSlimClient.Io.Commands;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedisSlimClient.Io
{
    class ConnectionPool : IConnection
    {
        readonly IReadOnlyCollection<IConnection> _connections;

        public ConnectionPool(IReadOnlyCollection<IConnection> connections)
        {
            _connections = connections;

            Id = _connections.Aggregate(new StringBuilder(), (s, c) => s.Append(c.Id).Append('.')).ToString();
        }

        public string Id { get; }

        public async Task<ICommandPipeline> RouteCommandAsync(ICommandIdentity command)
        {
            var availablePipelines = await Task.WhenAll(_connections.Select(c => c.RouteCommandAsync(command)));

            var candidate = availablePipelines.OrderBy(r => r.Metrics.Workload).FirstOrDefault();

            return candidate;
        }

        public void Dispose()
        {
            foreach (var conn in _connections)
            {
                conn.Dispose();
            }
        }
    }
}
