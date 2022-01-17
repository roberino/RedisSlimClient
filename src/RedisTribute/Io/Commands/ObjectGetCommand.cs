using RedisTribute.Configuration;
using RedisTribute.Serialization;
using RedisTribute.Types;

namespace RedisTribute.Io.Commands
{
    class ObjectGetCommand<T> : RedisCommand<(T value, bool found)>
    {
        readonly ISerializerSettings _configuration;

        public ObjectGetCommand(RedisKey key, ISerializerSettings config) : base("GET", false, key)
        {
            _configuration = config;
        }

        protected override (T value, bool found) TranslateResult(IRedisObject result)
        {
            if (result.Type == RedisType.Null)
            {
                return (default, false);
            }

            return (_configuration.Deserialize<T>(result), true);
        }
    }
}