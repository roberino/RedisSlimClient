using RedisTribute.Io.Server;

namespace RedisTribute.Configuration
{
    public interface IPasswordManager
    {
        string GetPassword(IRedisEndpoint redisEndpoint);
    }
}