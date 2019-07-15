using RedisSlimClient.Configuration;
using RedisSlimClient.Io.Net;

namespace RedisSlimClient.Io
{
    static class SocketFactory
    {
        public static IManagedSocket CreateSocket(ClientConfiguration configuration, IServerEndpointFactory endPointFactory)
        {
            if (!configuration.SslConfiguration.UseSsl)
            {
                return new SocketFacade(endPointFactory, configuration.ConnectTimeout);
            }

            return new SslSocket(endPointFactory, configuration.ConnectTimeout, configuration.SslConfiguration, configuration);
        }
    }
}