using RedisSlimClient.Configuration;
using System.IO;
using System.Linq;
using System.Net.Sockets;

namespace RedisSlimClient.Io
{
    class ConnectionFactory
    {
        public IConnection Create(ClientConfiguration configuration)
        {
            if (configuration.ConnectionPoolSize <= 1)
            {
                return CreateImpl(configuration);
            }

            return new ConnectionPool(Enumerable
                .Range(1, configuration.ConnectionPoolSize).Select(n => CreateImpl(configuration)).ToArray());
        }

        IConnection CreateImpl(ClientConfiguration configuration)
        {
            if (configuration.UseAsyncronousPipeline)
            {
                return new Connection(configuration.ServerUri.AsEndpoint(), s => new CommandPipeline(CreateStream(s)));
            }

            return new Connection(configuration.ServerUri.AsEndpoint(), s => new SyncCommandPipeline(CreateStream(s)));
        }

        static Stream CreateStream(Socket socket) => new NetworkStream(socket, FileAccess.ReadWrite);
    }
}