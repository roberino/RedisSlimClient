using RedisTribute.Configuration;
using RedisTribute.Serialization;
using RedisTribute.Serialization.Objects;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.Types.Streams
{
    class RedisStream<T> : IRedisStream<T>
    {
        readonly RedisKey _key;
        readonly IPrimativeStreamClient _client;
        readonly ISerializerSettings _serializerSettings;
        readonly IObjectSerializer<T> _serializer;


        public RedisStream(IPrimativeStreamClient client,
            ISerializerSettings serializerSettings, 
            RedisKey key)
        {
            _client = client;
            _serializerSettings = serializerSettings;
            _key = key;

            _serializer = SerializerFactory.Instance.Create<T>();
        }

        public async Task<StreamId> WriteAsync(T value, CancellationToken cancellation = default)
        {
            var objectWriter = new DictionaryObjectWriter(_serializerSettings);

            _serializer.WriteData(value, objectWriter);

            var data = objectWriter.Output;

            var id = await _client.XAddAsync(_key, data, cancellation);

            return id;
        }

        public async Task<bool> DeleteAsync(CancellationToken cancellation = default)
        {
            return (await  _client.DeleteAsync(_key.ToString(), cancellation)) > 0;
        }
    }
}
