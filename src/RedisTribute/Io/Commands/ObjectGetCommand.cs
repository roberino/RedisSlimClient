using RedisTribute.Configuration;
using RedisTribute.Serialization;
using RedisTribute.Types;
using System;

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

            if (result is RedisString strData)
            {
                var byteSeq = new ArraySegmentToRedisObjectReader(new StreamIterator(strData.ToStream()));
                var objReader = new ObjectReader(byteSeq, _configuration.Encoding, null, _configuration.SerializerFactory);

                return _serializer.ReadData(objReader, default);
            }

            throw new ArgumentException($"{result.Type}");
        }
    }
}