using System.Net;

namespace RedisSlimClient.Configuration
{
    public interface IHostAddressResolver
    {
        IHostAddressResolver Register(string host, string ip);

        IHostAddressResolver Register(IPHostEntry ipEntry);

        IHostAddressResolver Map(string cidrOrIpAddress, string targetIp);

        IPAddress Resolve(IPAddress ipAddress);

        IPHostEntry Resolve(string host);
    }
}