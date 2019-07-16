using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RedisSlimClient.Io.Commands;
using RedisSlimClient.Io.Server;

namespace RedisSlimClient.Io
{
    class ConnectionPool : IConnection
    {
        readonly IReadOnlyCollection<IConnection> _connections;

        public ConnectionPool(IReadOnlyCollection<IConnection> connections)
        {
            _connections = connections;

            Id = _connections.Aggregate(new StringBuilder(), (s, c) => s.Append(c.Id).Append('.')).ToString();

            EndPointInfo = new ServerEndPointInfo(null, 0);
        }

        public string Id { get; }

        public ServerEndPointInfo EndPointInfo { get; }

        public async Task<ICommandPipeline> RouteCommandAsync(ICommandIdentity command)
        {
            var results = await Task.WhenAll(_connections.Select(async c => new
            {
                cmd = c,
                score = await c.CalculateWorkLoad(command)
            }));

            var candidate = results.OrderBy(r => r.score).Select(c => c.cmd).FirstOrDefault();

            return await candidate.RouteCommandAsync(command);
        }

        public void Dispose()
        {
            foreach (var conn in _connections)
            {
                conn.Dispose();
            }
        }

        public async Task<float> CalculateWorkLoad(ICommandIdentity command)
        {
            var results = await Task.WhenAll(_connections.Select(c => c.CalculateWorkLoad(command)));

            return results.Min();
        }
    }
}
