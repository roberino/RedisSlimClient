using RedisTribute.Io.Commands;
using System;
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
            if(_connections.Count == 0)
                return Array.Empty<MultiKeyRoute>();

            var availablePipelines = await Task.WhenAll(_connections.Select(c => c.RouteMultiKeyCommandAsync(command)));

            return availablePipelines.OrderBy(x => x.Count()).First();
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
            if (_connections.Count == 0)
                throw new InvalidOperationException("No connections");

            var availablePipelines = await Task.WhenAll(_connections.Select(c => c.RouteCommandAsync(command)));

            var candidate = availablePipelines.OrderBy(r => r.Metrics.Workload).First();

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
