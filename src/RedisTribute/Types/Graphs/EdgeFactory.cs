using RedisTribute.Configuration;
using RedisTribute.Serialization;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.Types.Graphs
{
    class EdgeFactory<T>
    {
        readonly NameResolver _nameResolver;
        readonly IHashSetClient _hashClient;
        readonly ISerializerSettings _serializerSettings;
        readonly Func<string, CancellationToken, Task<IVertex<T>>> _vertexLookup;
        readonly IObjectSerializer<EdgeData> _edgeSerializer;

        public EdgeFactory(NameResolver nameResolver, IHashSetClient hashClient, ISerializerSettings serializerSettings, Func<string, CancellationToken, Task<IVertex<T>>> vertexLookup)
        {
            _nameResolver = nameResolver;
            _hashClient = hashClient;
            _serializerSettings = serializerSettings;
            _vertexLookup = vertexLookup;
            _edgeSerializer = _serializerSettings.SerializerFactory.Create<EdgeData>();
        }

        public byte[] Serialize(Edge<T> edge)
        {
            return _serializerSettings.SerializeAsBytes(edge.Data);
        }

        public Edge<T> Create(byte[] data)
        {
            var edgeData = _serializerSettings.Deserialize(_edgeSerializer, data);
            var location = _nameResolver.GetLocation(GraphObjectType.Edge, edgeData.Id);

            return new Edge<T>(location, edgeData, c => _vertexLookup(edgeData.TargetVertexId, c));
        }

        public Edge<T> Create(string edgeId, string vertexId, string? edgeLabel = null, Direction direction = Direction.Out, double weight = 1)
        {
            var data = new EdgeData() { Direction = direction, Label = edgeLabel ?? string.Empty, Weight = weight, TargetVertexId = vertexId, Id = edgeId };

            var edgeUri = _nameResolver.GetLocation(GraphObjectType.Edge, data.Id);

            return new Edge<T>(edgeUri, data, c => _vertexLookup(vertexId, c), true);
        }
    }
}
