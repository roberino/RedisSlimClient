using Newtonsoft.Json;
using RedisTribute.Configuration;
using RedisTribute.Json;

namespace RedisTribute
{
    public static class JsonClientExtensions
    {
        public static ClientConfiguration UseJsonSerialization(this ClientConfiguration configuration, JsonSerializerSettings settings = null)
        {
            configuration.SerializerFactory = new JsonSerializerAdapter(configuration.Encoding, settings);

            return configuration;
        }
    }
}