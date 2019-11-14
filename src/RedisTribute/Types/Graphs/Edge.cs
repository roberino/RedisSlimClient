using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.Types.Graphs
{
    public readonly struct Edge<T>
    {
        private readonly Func<CancellationToken, Task<IVertex<T>>> _vertexLink;

        public Edge(string label, double weight, Func<CancellationToken, Task<IVertex<T>>> vertexLink)
        {
            Label = label;
            Weight = weight;
            _vertexLink = vertexLink;
        }

        public string Label { get; }
        public double Weight { get; }

        public Task<IVertex<T>> GetVertex(CancellationToken cancellation) => _vertexLink(cancellation);
    }
}