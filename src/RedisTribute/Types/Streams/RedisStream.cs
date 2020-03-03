using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RedisTribute.Configuration;
using RedisTribute.Serialization;

namespace RedisTribute.Types.Streams
{
    class RedisStream<T>
    {
        readonly RedisKey _key;
        readonly IStreamClient _client;
        readonly ISerializerSettings _serializerSettings;
        readonly IObjectSerializer<T> _serializer;


        public RedisStream(IStreamClient client,
            ISerializerSettings serializerSettings, 
            RedisKey key)
        {
            _client = client;
            _serializerSettings = serializerSettings;
            _key = key;

            _serializer = serializerSettings.SerializerFactory.Create<T>();
        }

        public Task<StreamId> WriteAsync(T value)
        {
            throw new NotImplementedException();
        }
    }
}
