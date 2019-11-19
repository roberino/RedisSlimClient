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
        readonly string _graphNamespace;
        readonly ISerializerSettings _serializerSettings;
        readonly Func<string, CancellationToken, Task<IVertex<T>>> _lookup;
        readonly IPersistentDictionary<byte[]> _nodeData;
        readonly List<Edge<T>> _edges;

        public Vertex(string graphNamespace, string label, ISerializerSettings serializerSettings, IPersistentDictionary<byte[]> nodeData, Func<string, CancellationToken, Task<IVertex<T>>> lookup)
        {
            _serializerSettings = serializerSettings;
            _nodeData = nodeData;
            _lookup = lookup;

            var data = GetVertexData(nodeData);

            _edges = data.Edges;
            _graphNamespace = graphNamespace;
            Label = label;
            Attributes = data.Attributes;
        }

        public string Namespace { get; }
        public string Label { get; }
        public T Attributes { get; }
        public IReadOnlyCollection<Edge<T>> Edges => _edges;

        public async Task AcceptAsync(IVisitor<T> visitor, CancellationToken cancellation = default)
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

                var tasks = _edges.Select(async e =>
                {
                    if (!await visitor.ShouldTraverseAsync(e, cancellation))
                    {
                        return false;
                    }

                    var ev = await e.GetVertex(cancellation);

                    return await visitor.VisitAsync(ev, cancellation);
                });

                await Task.WhenAll(tasks);
            }
        }

        public Edge<T> Connect(string label, double weight = 1)
        {
            lock (_edges)
            {
                if (_edges.Any(e => string.Equals(e.Label, label)))
                {
                    throw new InvalidOperationException($"Edge already exists: {label}");
                }

                var newEdge = new Edge<T>(label, weight, c => _lookup(label, c));

                var weightData = _serializerSettings.SerializeAsBytes(weight);

                _nodeData[GetEdgeKey(label)] = weightData;
                _edges.Add(newEdge);

                return newEdge;
            }
        }

        public void Remove(string label)
        {
            lock (_edges)
            {
                var key = GetEdgeKey(label);

                _nodeData.Remove(key);
            }
        }

        public Task SaveAsync(CancellationToken cancellation = default)
        {
            return _nodeData.SaveAsync(true, cancellation);
        }

        (T Attributes, List<Edge<T>> Edges) GetVertexData(IPersistentDictionary<byte[]> nodeData)
        {
            var serializer = _serializerSettings.SerializerFactory.Create<T>();

            T attributes = default;

            if (nodeData.TryGetValue(GetMetaKey(nameof(Vertex<object>.Attributes)), out var data))
            {
                attributes = _serializerSettings.Deserialize(serializer, data);
            }

            var weightSerializer = _serializerSettings.SerializerFactory.Create<double>();

            var edges = nodeData.Where(kv => IsEdgeKey(kv.Key)).Select(e =>
            {
                var weight = _serializerSettings.Deserialize(weightSerializer, e.Value);

                return new Edge<T>(GetEdgeName(e.Key), weight, c => _lookup(e.Key, c));
            })
            .ToList();

            return (attributes, edges);
        }

        public bool Equals(IVertex<T> other)
        {
            if (other == null)
            {
                return false;
            }

            return string.Equals(Namespace, other.Namespace) && string.Equals(other.Label, Label);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as IVertex<T>);
        }

        public override int GetHashCode()
        {
            return $"{Namespace}:{Label}".GetHashCode();
        }

        static string GetMetaKey(string keyName) => $"${keyName}";
        static string GetEdgeKey(string keyName) => $">{keyName}";
        static string GetEdgeName(string edgeKey) => edgeKey.Substring(1);
        static bool IsMetaKey(string keyName) => keyName.StartsWith("$");
        static bool IsEdgeKey(string keyName) => keyName.StartsWith(">");
    }
}
