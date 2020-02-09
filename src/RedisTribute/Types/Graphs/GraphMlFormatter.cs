using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RedisTribute.Types.Graphs
{
    class GraphMlFormatter<T>
    {
        //    <graphml xmlns = "http://graphml.graphdrawing.org/xmlns"
        //xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
        //xsi:schemaLocation="http://graphml.graphdrawing.org/xmlns
        // http://graphml.graphdrawing.org/xmlns/1.0/graphml.xsd">

        const string MainNs = "http://graphml.graphdrawing.org/xmlns";
        const string XsiNs = "http://www.w3.org/2001/XMLSchema-instance";
        const string SchemUri = "http://graphml.graphdrawing.org/xmlns";

        public async Task<XDocument> Format(ITraversable<T> traversable, CancellationToken cancellation)
        {
            var doc = new XDocument(new XElement(XName.Get("graphml", MainNs), new XAttribute("edgedefault", "directed")));

            var visitor = new NodeVisitor(doc.Root);

            await traversable.TraverseAsync(visitor, cancellation);

            return doc;
        }

        class NodeVisitor : IVisitor<T>
        {
            readonly HashSet<string> _visitedVertexes;
            readonly HashSet<string> _visitedEdges;
            readonly XElement _root;

            public NodeVisitor(XElement root)
            {
                _root = root;
                _visitedVertexes = new HashSet<string>();
                _visitedEdges = new HashSet<string>();
            }

            public Task<bool> ShouldTraverseAsync(IEdge<T> edge, CancellationToken cancellation)
            {
                lock (_visitedEdges)
                {
                    if (_visitedEdges.Contains(edge.Id))
                    {
                        return Task.FromResult(false);
                    }

                    _visitedEdges.Add(edge.Id);

                    return Task.FromResult(true);
                }
            }

            public async Task<bool> VisitAsync(IVertex<T> vertex, CancellationToken cancellation)
            {
                const string vertexXName = "node";

                lock (_visitedVertexes)
                {
                    if (_visitedVertexes.Contains(vertex.Id))
                    {
                        return false;
                    }
                    _visitedVertexes.Add(vertex.Id);
                }

                var nodeX = new XElement(XName.Get(vertexXName, MainNs), new XAttribute("id", vertex.Id));

                if (vertex.Label != null)
                {
                    AddData(nodeX, nameof(vertex.Label), vertex.Label);
                }

                _root.Add(nodeX);
                
                foreach(var edge in vertex.Edges)
                {
                    if (edge.Direction == Direction.Out || edge.Direction == Direction.Bidirectional)
                    {
                        AddEdge(edge.Id, vertex.Id, edge.TargetVertex.Id);
                    }
                    if (edge.Direction == Direction.In || edge.Direction == Direction.Bidirectional)
                    {
                        AddEdge(edge.Id, edge.TargetVertex.Id, vertex.Id);
                    }
                }

                return true;
            }

            private void AddData(XElement parent, string key, string content)
            {
                //<data key="d0">blue</data>

                parent.Add(new XElement(XName.Get("data", MainNs), new XAttribute(nameof(key), key.ToLower()),  content));
            }

            private void AddEdge(string id, string source, string target)
            {
                const string edgeXName = "edge";

                var edgeX = new XElement(XName.Get(edgeXName, MainNs), new XAttribute(nameof(id), id), new XAttribute(nameof(source), source), new XAttribute(nameof(target), target));

                _root.Add(edgeX);
            }
        }
    }
}