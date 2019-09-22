using RedisTribute.Io.Commands;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RedisTribute.Io
{
    class ConnectionPool : ICommandRouter
    {
        readonly IReadOnlyCollection<ICommandRouter> _connections;

        public ConnectionPool(IReadOnlyCollection<ICommandRouter> connections)
        {
            _connections = connections;
        }

        public async Task<IReadOnlyCollection<MultiKeyRoute>> RouteMultiKeyCommandAsync(IMultiKeyCommandIdentity command)
        {
            var availablePipelines = (await Task.WhenAll(_connections.Select(c => c.RouteMultiKeyCommandAsync(command)))).ToArray();

            return availablePipelines.OrderBy(x => x.Count()).FirstOrDefault();
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
