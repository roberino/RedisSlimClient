using RedisSlimClient.Configuration;
using RedisSlimClient.Serialization;
using RedisSlimClient.Types;
using System;
using System.IO;

namespace RedisSlimClient.Io.Commands
{
    class ObjectSetCommand<T> : RedisCommand<bool>
    {
        private readonly ISerializerSettings _configuration;
        private readonly T _objectData;
        private readonly IObjectSerializer<T> _serializer;

        public ObjectSetCommand(RedisKey key, ISerializerSettings config, T objectData) : base("SET", true, key)
        {
            _configuration = config;
            _objectData = objectData;
            _serializer = config.SerializerFactory.Create<T>();
        }

        public override object[] GetArgs() => new object[] { CommandText, Key.Bytes, GetObjectData() };

        protected override bool TranslateResult(IRedisObject redisObject) => string.Equals(redisObject.ToString(), "OK", StringComparison.OrdinalIgnoreCase);

        byte[] GetObjectData()
        {
            using (var ms = new MemoryStream())
            {
                var objWriter = new ObjectWriter(ms, _configuration.Encoding, null, _configuration.SerializerFactory);

                _serializer.WriteData(_objectData, objWriter);

                return ms.ToArray();
            }
        }
    }
}