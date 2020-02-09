using RedisTribute.Configuration;
using RedisTribute.Types;

namespace RedisTribute
{
    static class ResultConvertion
    {
        public static string AsString(IRedisObject redisString, ISerializerSettings settings)
        {
            if (redisString == null || redisString.Type == RedisType.Null)
            {
                return null;
            }

            using (var value = (RedisString)redisString)
            {
                return value.ToString(settings.Encoding);
            }
        }

        public static long AsLong(IRedisObject redisValue, ISerializerSettings settings) => redisValue.ToLong();

        public static byte[] AsBytes(IRedisObject redisString, ISerializerSettings settings)
        {
            if (redisString == null || redisString.Type == RedisType.Null)
            {
                return null;
            }

            using (var value = (RedisString)redisString)
            {
                return value.Value;
            }
        }
    }
}