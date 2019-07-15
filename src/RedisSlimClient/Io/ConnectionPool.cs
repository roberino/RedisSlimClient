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

        public float WorkLoad => _connections.Average(c => c.WorkLoad);

        public ServerEndPointInfo EndPointInfo { get; }

        public Task<ICommandPipeline> RouteCommandAsync(ICommandIdentity command)
        {
            var candidate = _connections.OrderBy(c => c.WorkLoad).FirstOrDefault();

            return candidate.RouteCommandAsync(command);
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
