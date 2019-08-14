using RedisSlimClient.Io.Commands;
using RedisSlimClient.Types;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RedisSlimClient.Io
{
    class ConnectionPool : ICommandRouter
    {
        readonly IReadOnlyCollection<ICommandRouter> _connections;

        public ConnectionPool(IReadOnlyCollection<ICommandRouter> connections)
        {
            _connections = connections;
        }

        public async Task<IEnumerable<MultiKeyRoute>> RouteMultiKeyCommandAsync(IMultiKeyCommandIdentity command)
        {
            var finalSelection = new Dictionary<ICommandExecutor, RedisKey[]>();

            var availablePipelines = (await Task.WhenAll(_connections.Select(c => c.RouteMultiKeyCommandAsync(command))))
                .SelectMany(x => x)
                .OrderByDescending(x => x.Keys.Length).ToList();

            var assigned = new HashSet<RedisKey>(command.Keys);

            foreach(var pipeGroup in availablePipelines)
            {
                foreach(var k in pipeGroup.Keys)
                {
                    if (assigned.Contains(k))
                    {

                    }
                }
            }

            throw new System.NotImplementedException();
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
