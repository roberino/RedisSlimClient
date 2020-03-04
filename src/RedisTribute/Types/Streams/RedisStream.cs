using System;
using System.Collections.Generic;
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

        public async Task<StreamEntryId> WriteAsync(T value, CancellationToken cancellation = default)
        {
            var objectWriter = new DictionaryObjectWriter(_serializerSettings);

            _serializer.WriteData(value, objectWriter);

            var data = objectWriter.Output;

            var id = await _client.XAddAsync(_key, data, cancellation);

            return id;
        }

        public async Task ReadAllAsync(Func<KeyValuePair<StreamEntryId, T>, Task> processor, bool exitWhenNoData = true, int batchSize = 100, CancellationToken cancellation = default)
        {
            StreamEntryId currentStart = StreamEntryId.Start;

            while (!cancellation.IsCancellationRequested)
            {
                var results =
                    await _client.XRangeAsync(_key, currentStart, StreamEntryId.End, batchSize, cancellation);

                if (results.Length == 0)
                {
                    if (exitWhenNoData)
                    {
                        return;
                    }

                    await Task.Delay(5, cancellation);
                    
                    continue;
                }

                foreach (var result in results)
                {
                    var reader = new DictionaryObjectReader(result.data, _serializerSettings);

                    var item = _serializer.ReadData(reader, default);

                    await processor(new KeyValuePair<StreamEntryId, T>(result.id, item));
                }

                currentStart = results[results.Length - 1].id.Next();
            }
        }

        public async Task<bool> DeleteAsync(CancellationToken cancellation = default)
        {
            return (await  _client.DeleteAsync(_key.ToString(), cancellation)) > 0;
        }
    }
}
