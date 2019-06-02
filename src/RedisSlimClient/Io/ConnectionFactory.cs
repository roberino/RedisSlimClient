using RedisSlimClient.Configuration;
using System.Linq;

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
                return new Connection(
                    configuration.ServerUri.AsEndpoint(),
                    configuration.ConnectTimeout,
                    configuration.TelemetryWriter,
                    async s => new CommandPipeline(await s.CreateStreamAsync(configuration.ConnectTimeout), configuration.TelemetryWriter));
            }

            return new Connection(
                configuration.ServerUri.AsEndpoint(),
                configuration.ConnectTimeout,
                    configuration.TelemetryWriter,
                async s => new SyncCommandPipeline(await s.CreateStreamAsync(configuration.ConnectTimeout)));
        }
    }
}