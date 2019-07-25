using RedisSlimClient.Io.Commands;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RedisSlimClient.Io
{
    class ConnectionPool : IConnection
    {
        readonly IReadOnlyCollection<IConnection> _connections;

        public ConnectionPool(IReadOnlyCollection<IConnection> connections)
        {
            _connections = connections;
        }

        public async Task<IEnumerable<ICommandExecutor>> RouteCommandAsync(ICommandIdentity command, ConnectionTarget target)
        {
            var availablePipelines = (await Task.WhenAll(_connections.Select(c => c.RouteCommandAsync(command, target)))).SelectMany(x => x);

            if (target == ConnectionTarget.FirstAvailable)
            {
                return availablePipelines.Take(1);
            }

            return availablePipelines;
        }

        public async Task<ICommandExecutor> RouteCommandAsync(ICommandIdentity command)
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
