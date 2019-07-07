using RedisSlimClient.Configuration;
using RedisSlimClient.Io.Pipelines;
using RedisSlimClient.Io.Net;
using System.Net;

namespace RedisSlimClient.Io
{
    static class SocketFactory
    {
        public static IManagedSocket CreateSocket(ClientConfiguration configuration, EndPoint endPoint)
        {
            if (!configuration.SslConfiguration.UseSsl)
            {
                return new SocketFacade(endPoint, configuration.ConnectTimeout);
            }

            return new SslSocket(endPoint, configuration.ConnectTimeout, configuration.SslConfiguration, configuration);
        }
    }
}