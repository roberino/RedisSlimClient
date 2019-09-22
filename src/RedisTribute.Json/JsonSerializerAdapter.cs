using Newtonsoft.Json;
using RedisTribute.Serialization;
using RedisTribute.Types.Primatives;
using System.IO;
using System.Text;

namespace RedisTribute.Json
{
    class JsonSerializerAdapter : IObjectSerializerFactory
    {
        readonly JsonSerializerSettings _settings;

        public JsonSerializerAdapter(JsonSerializerSettings settings = null)
        {
            _settings = settings ?? new JsonSerializerSettings();
        }

        public IObjectSerializer<T> Create<T>() => new ObjectSerializer<T>(_settings);

        class ObjectSerializer<T> : IObjectSerializer<T>
        {
            readonly JsonSerializer _jsonSerializer;

            public ObjectSerializer(JsonSerializerSettings settings)
            {
                _jsonSerializer = JsonSerializer.Create(settings);
            }

            public T ReadData(IObjectReader reader, T defaultValue)
            {
                var bytes = reader.Raw();

                using (var stream = new MemoryStream(bytes))
                using (var streamReader = new StreamReader(stream, Encoding.UTF8))
                using (var jsonReader = new JsonTextReader(streamReader))
                {
                    return _jsonSerializer.Deserialize<T>(jsonReader);
                }
            }

            public void WriteData(T instance, IObjectWriter writer)
            {
                var bufferSize = 1024 * 8;
                var transfer = false;

                while (true)
                {
                    try
                    {
                        using (var stream = StreamPool.Instance.GetStream(bufferSize))
                        {
                            using (var streamWriter = new StreamWriter(stream, Encoding.UTF8))
                            {
                                _jsonSerializer.Serialize(streamWriter, instance);

                                streamWriter.Flush();

                                var buffer = stream.GetBuffer();

                                transfer = true;

                                writer.Raw(buffer.Array, buffer.Count);

                                return;
                            }
                        }
                    }
                    catch
                    {
                        if (transfer)
                        {
                            throw;
                        }
                        bufferSize = bufferSize << 1;
                    }
                }
            }
        }
    }
}