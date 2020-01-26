using RedisTribute.Configuration;
using RedisTribute.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.Types.Graphs
{
    class Vertex<T> : IVertex<T>
    {
        readonly NameResolver _nameResolver;
        readonly ISerializerSettings _serializerSettings;
        readonly IPersistentDictionary<byte[]> _nodeData;
        readonly EdgeFactory<T> _edgeFactory;
        readonly List<Edge<T>> _edges;

        public Vertex(NameResolver nameResolver, string id, ISerializerSettings serializerSettings, IPersistentDictionary<byte[]> nodeData, EdgeFactory<T> edgeFactory)
        {
            _serializerSettings = serializerSettings;
            _nodeData = nodeData;
            _edgeFactory = edgeFactory;
            _nameResolver = nameResolver;

            var data = GetVertexData(nodeData, id);

            _edges = data.Edges;

            Id = id;
            Attributes = data.Attributes;
            Label = data.Label;
        }

        public event Action Saved;

        public string Namespace => _nameResolver.Namespace;
        public string Id { get; }
        public string Label { get; set; }
        public T Attributes { get; set; }
        public IReadOnlyCollection<IEdge<T>> Edges => _edges.Where(e => !e.Removed).ToList();

        public async Task TraverseAsync(IVisitor<T> visitor, CancellationToken cancellation = default)
        {
            if (cancellation.IsCancellationRequested)
            {
                return;
            }

            if (await visitor.VisitAsync(this, cancellation))
            {
                if (cancellation.IsCancellationRequested)
                {
                    return;
                }

                var tasks = _edges.Where(e => !e.Removed).Select(async e =>
                {
                    if (!await visitor.ShouldTraverseAsync(e, cancellation))
                    {
                        return false;
                    }

                    var ev = await e.TargetVertex.GetVertex(cancellation);

                    return await visitor.VisitAsync(ev, cancellation);
                });

                await Task.WhenAll(tasks);
            }
        }

        public IEdge<T> Connect(string vertexId, string edgeLabel = null, Direction direction = Direction.Out, double weight = 1)
            => Connect(CreateEdgeId(), vertexId, edgeLabel, direction, weight);

        public IEdge<T> Connect(string edgeId, string vertexId, string edgeLabel = null, Direction direction = Direction.Out, double weight = 1)
        {
            lock (_edges)
            {
                if (_edges.Any(e => string.Equals(e.Label, edgeLabel) && string.Equals(e.TargetVertex.Id, vertexId)))
                {
                    throw new InvalidOperationException($"Edge already exists: {edgeLabel} => {vertexId}");
                }

                var newEdge = _edgeFactory.Create(edgeId, vertexId, edgeLabel, direction, weight);

                _edges.Add(newEdge);

                return newEdge;
            }
        }

        public async Task SaveAsync(CancellationToken cancellation = default)
        {
            _nodeData[_nameResolver.GetLocation(GraphObjectType.Metadata, Id).PathAndQuery] = _serializerSettings.SerializeAsBytes(Attributes);
            _nodeData[_nameResolver.GetLocation(GraphObjectType.Label, Id).PathAndQuery] = _serializerSettings.SerializeAsBytes(Label);

            await UpdateEdges(true, cancellation);

            Saved?.Invoke();
        }

        public async Task UpdateEdges(bool traverse, CancellationToken cancellation = default)
        {
            var modified = _edges.Where(e => e.Dirty).Select(e => new
            {
                uri = _nameResolver.GetLocation(GraphObjectType.Edge, e.Id).PathAndQuery,
                edge = e
            })
              .ToArray();

            var updatedVertexes = new List<Vertex<T>>();

            foreach (var item in modified)
            {
                if (item.edge.Removed)
                {
                    _nodeData.Remove(item.uri);

                    if (traverse)
                    {
                        var v = (Vertex<T>)await item.edge.TargetVertex.GetVertex(cancellation);

                        var mirrorEdge = v.Edges.SingleOrDefault(e => e.Id == item.edge.Id);

                        if (mirrorEdge == null)
                        {
                            continue;
                        }

                        mirrorEdge.Remove();

                        updatedVertexes.Add(v);
                    }
                }
                else
                {
                    _nodeData[item.uri] = _edgeFactory.Serialize(item.edge);

                    if (traverse)
                    {
                        var v = (Vertex<T>)await item.edge.TargetVertex.GetVertex(cancellation);

                        var mirrorEdge = (Edge<T>)v.Edges.SingleOrDefault(e => e.Id == item.edge.Id);

                        if (mirrorEdge == null)
                        {
                            v.Connect(item.edge.Id, Id, item.edge.Label, item.edge.Direction.Compliment(), item.edge.Weight);
                        }
                        else
                        {
                            mirrorEdge.Direction = item.edge.Direction.Compliment();
                            mirrorEdge.Label = item.edge.Label;
                            mirrorEdge.Weight = item.edge.Weight;
                        }

                        updatedVertexes.Add(v);
                    }
                }
            }

            await _nodeData.SaveAsync(true, cancellation);

            if (traverse && updatedVertexes.Any())
            {
                await Task.WhenAll(updatedVertexes.GroupBy(v => v.Id).Select(v => v.First().UpdateEdges(false, cancellation)));
            }

            foreach (var item in modified)
            {
                if (item.edge.Removed)
                {
                    _edges.Remove(item.edge);
                }

                item.edge.Clean();
            }
        }

        string CreateEdgeId()
        {
            while (true)
            {
                var id = Guid.NewGuid().ToString("N").Substring(0, 4);

                if (!_edges.Any(e => e.Id == id))
                {
                    return id;
                }
            }
        }

        (T Attributes, List<Edge<T>> Edges, string Label) GetVertexData(IPersistentDictionary<byte[]> nodeData, string id)
        {
            var serializer = _serializerSettings.SerializerFactory.Create<T>();

            string label = null;
            T attributes = default;

            if (nodeData.TryGetValue(_nameResolver.GetLocation(GraphObjectType.Metadata, id).PathAndQuery, out var data))
            {
                attributes = _serializerSettings.Deserialize(serializer, data);
            }

            if (nodeData.TryGetValue(_nameResolver.GetLocation(GraphObjectType.Label, id).PathAndQuery, out var labelData))
            {
                label = _serializerSettings.Deserialize<string>(labelData);
            }

            var edgeSerializer = _serializerSettings.SerializerFactory.Create<EdgeData>();

            var edges = nodeData
                .Where(kv => _nameResolver.IsType(kv.Key, GraphObjectType.Edge))
                .Select(e => _edgeFactory.Create(e.Value))
                .ToList();

            return (attributes, edges, label);
        }

        public bool Equals(IVertex<T> other)
        {
            if (other == null)
            {
                return false;
            }

            return string.Equals(Namespace, other.Namespace) && string.Equals(other.Id, Id);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as IVertex<T>);
        }

        public override int GetHashCode()
        {
            return $"{Namespace}:{Id}".GetHashCode();
        }
    }
}
