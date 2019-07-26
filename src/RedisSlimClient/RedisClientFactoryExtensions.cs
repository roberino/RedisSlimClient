using RedisSlimClient.Configuration;

namespace RedisSlimClient
{
    public static class RedisClientFactoryExtensions
    {
        public static IRedisReader CreateReader(this ClientConfiguration configuration)
        {
            return RedisClient.Create(configuration);
        }

        public static IRedisClient CreateClient(this ClientConfiguration configuration)
        {
            return RedisClient.Create(configuration);
        }
    }
}