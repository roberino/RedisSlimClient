using RedisTribute.Configuration;
using RedisTribute.Serialization;
using RedisTribute.Types;
using RedisTribute.Types.Primatives;
using System;
using System.IO;

namespace RedisTribute.Io.Commands
{
    class ObjectSetCommand<T> : RedisCommand<bool>
    {
        static readonly object _lockObj = new object();
        static int _maxBufferSize = 512;

        readonly ISerializerSettings _configuration;
        readonly IObjectSerializer<T> _serializer;
        readonly T _objectData;

        public ObjectSetCommand(RedisKey key, ISerializerSettings config, T objectData) : base("SET", true, key)
        {
            _configuration = config;
            _objectData = objectData;
            _serializer = config.SerializerFactory.Create<T>();
        }

        protected override CommandParameters GetArgs()
        {
            var objStream = GetObjectData();

            return new CommandParameters(() => objStream.Dispose(), CommandText, Key.Bytes, objStream.GetBuffer());
        }

        protected override bool TranslateResult(IRedisObject redisObject) => string.Equals(redisObject.ToString(), "OK", StringComparison.OrdinalIgnoreCase);

        byte[] GetObjectDataV1()
        {
            using (var ms = new MemoryStream())
            {
                var objWriter = new ObjectWriter(ms, _configuration.Encoding, null, _configuration.SerializerFactory);

                _serializer.WriteData(_objectData, objWriter);

                return ms.ToArray();
            }
        }

        PooledStream GetObjectData()
        {
            var ms = StreamPool.Instance.GetStream(_maxBufferSize);

            try
            {
                var objWriter = new ObjectWriter(ms, _configuration.Encoding, null, _configuration.SerializerFactory);

                _serializer.WriteData(_objectData, objWriter);

                return ms;
            }
            catch (NotSupportedException)
            {
                ms.Dispose();

                lock (_lockObj)
                {
                    _maxBufferSize = _maxBufferSize << 1;

                    if (_maxBufferSize > 16000)
                    {
                        throw new NotSupportedException();
                    }
                }
            }

            return GetObjectData();
        }
    }
}