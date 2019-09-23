using Newtonsoft.Json;
using RedisTribute.Serialization;
using RedisTribute.Types.Primatives;
using System.IO;
using System.Text;

namespace RedisTribute.Json
{
    class JsonSerializerAdapter : IObjectSerializerFactory
    {
        readonly Encoding _encoding;
        readonly JsonSerializerSettings _settings;

        public JsonSerializerAdapter(Encoding encoding, JsonSerializerSettings settings = null)
        {
            _encoding = encoding;
            _settings = settings ?? new JsonSerializerSettings();
        }

        public IObjectSerializer<T> Create<T>() => new ObjectSerializer<T>(_settings, _encoding);

        class ObjectSerializer<T> : IObjectSerializer<T>
        {
            readonly JsonSerializer _jsonSerializer;
            readonly Encoding _encoding;

            public ObjectSerializer(JsonSerializerSettings settings, Encoding encoding)
            {
                _jsonSerializer = JsonSerializer.Create(settings);
                _encoding = encoding;
            }

            public T ReadData(IObjectReader reader, T defaultValue)
            {
                using (var stream = reader.Raw())
                using (var streamReader = new StreamReader(stream, _encoding))
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
                        using (var stream = StreamPool.Instance.CreateWritable(bufferSize))
                        {
                            using (var streamWriter = new StreamWriter(stream, _encoding))
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