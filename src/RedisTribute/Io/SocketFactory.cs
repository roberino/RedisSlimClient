using RedisTribute.Configuration;
using RedisTribute.Io.Net;

namespace RedisTribute.Io
{
    static class SocketFactory
    {
        public static IManagedSocket CreateSocket(ClientConfiguration configuration, IServerEndpointFactory endPointFactory)
        {
            if (!configuration.SslConfiguration.UseSsl)
            {
                return new SocketFacade(endPointFactory, configuration.DefaultOperationTimeout);
            }

            return new SslSocket(endPointFactory, configuration.ConnectTimeout, configuration.SslConfiguration, configuration);
        }
    }
}