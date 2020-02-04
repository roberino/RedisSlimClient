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

        public async Task<IVertex<T>> GetVertexAsync<T>(string id, CancellationToken cancellation = default)
        {
            var nameResolver = new NameResolver(_options.Namespace);

            nameResolver.ValidateId(id);

            var uri = nameResolver.GetLocation(GraphObjectType.Vertex, id);
            var edgeFactory = new EdgeFactory<T>(nameResolver, null, _serializerSettings, GetVertexAsync<T>);
            var nodeData = await _client.GetHashSetAsync<byte[]>(uri.ToString(), cancellation);

            return new Vertex<T>(nameResolver, id, _serializerSettings, nodeData, edgeFactory);
        }
    }
}