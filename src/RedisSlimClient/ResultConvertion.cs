using RedisSlimClient.Configuration;
using RedisSlimClient.Types;

namespace RedisSlimClient
{
    static class ResultConvertion
    {
        public static string AsString(IRedisObject redisString, ISerializerSettings settings) => ((RedisString)redisString).ToString(settings.Encoding);
        public static byte[] AsBytes(IRedisObject redisString, ISerializerSettings settings) => ((RedisString)redisString).Value;
    }
}