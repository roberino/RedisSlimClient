using System.Net;

namespace RedisSlimClient.Configuration
{
    public interface IDnsResolver
    {
        IDnsResolver Register(string host, string ip);

        IDnsResolver Register(IPHostEntry ipEntry);

        IPHostEntry Resolve(string host);
    }
}