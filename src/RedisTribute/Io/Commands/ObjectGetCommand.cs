using RedisTribute.Configuration;
using RedisTribute.Serialization;
using RedisTribute.Types;

namespace RedisTribute.Io.Commands
{
    class ObjectGetCommand<T> : RedisCommand<(T value, bool found)>
    {
        static readonly bool IsPrimative = typeof(T).IsValueType;

        readonly ISerializerSettings _configuration;
        readonly IObjectSerializer<T> _serializer;

        public ObjectGetCommand(RedisKey key, ISerializerSettings config) : base("GET", false, key)
        {
            _configuration = config;
            _serializer = config.SerializerFactory.Create<T>();
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