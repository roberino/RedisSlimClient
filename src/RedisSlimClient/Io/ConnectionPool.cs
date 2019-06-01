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

        public float WorkLoad => _connections.Average(c => c.WorkLoad);

        public Task<ICommandPipeline> ConnectAsync()
        {
            var candidate = _connections.OrderBy(c => c.WorkLoad).FirstOrDefault();

            return candidate.ConnectAsync();
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
