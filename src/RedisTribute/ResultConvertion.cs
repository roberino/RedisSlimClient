using RedisTribute.Configuration;
using RedisTribute.Types;

namespace RedisTribute
{
    static class ResultConvertion
    {
        public static string AsString(IRedisObject redisString, ISerializerSettings settings) => ((RedisString)redisString).ToString(settings.Encoding);
        public static byte[] AsBytes(IRedisObject redisString, ISerializerSettings settings) => ((RedisString)redisString).Value;
    }
}