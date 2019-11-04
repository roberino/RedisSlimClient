using RedisTribute.Configuration;
using RedisTribute.Serialization;
using RedisTribute.Types;

namespace RedisTribute.Io.Commands
{
    class ObjectGetCommand<T> : RedisCommand<T>
    {
        static readonly bool IsPrimative = typeof(T).IsValueType;

        readonly ISerializerSettings _configuration;
        readonly IObjectSerializer<T> _serializer;

        public ObjectGetCommand(RedisKey key, ISerializerSettings config) : base("GET", false, key)
        {
            _configuration = config;
            _serializer = config.SerializerFactory.Create<T>();
        }

        protected override T TranslateResult(IRedisObject result)
        {
            if (result.Type == RedisType.Null)
            {
                if (IsPrimative)
                {
                    return default;
                }

                throw new KeyNotFoundException();
            }

            return _configuration.Deserialize<T>(result);
        }
    }
}