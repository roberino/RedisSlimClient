using RedisTribute.Configuration;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.Types.Graphs
{
    class Graph : IGraph
    {
        readonly IPersistentDictionaryProvider _client;
        readonly ISerializerSettings _serializerSettings;
        readonly GraphOptions _options;

        public Graph(IPersistentDictionaryProvider client, ISerializerSettings serializerSettings, GraphOptions options = default)
        {
            _client = client;
            _serializerSettings = serializerSettings;
            _options = options;
        }

        public async Task<IVertex<T>> GetVertexAsync<T>(string label, CancellationToken cancellation = default)
        {
            var nodeData = await _client.GetHashSetAsync<byte[]>(_options.GetKey(label), cancellation);

            return new Vertex<T>(_options.Namespace, label, _serializerSettings, nodeData, GetVertexAsync<T>);
        }
    }
}