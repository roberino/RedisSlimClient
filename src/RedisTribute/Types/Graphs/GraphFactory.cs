using RedisTribute.Configuration;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.Types.Graphs
{
    class GraphFactory
    {
        readonly IPersistentDictionaryClient _client;
        readonly ISerializerSettings _serializerSettings;

        public GraphFactory(IPersistentDictionaryClient client, ISerializerSettings serializerSettings)
        {
            _client = client;
            _serializerSettings = serializerSettings;
        }

        public async Task<IVertex<T>> GetVertexAsync<T>(string label, CancellationToken cancellation = default)
        {
            var nodeData = await _client.GetHashSetAsync<byte[]>(label, cancellation);

            return new Vertex<T>(label, _serializerSettings, nodeData, GetVertexAsync<T>);
        }
    }
}