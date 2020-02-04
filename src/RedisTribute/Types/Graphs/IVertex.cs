using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.Types.Graphs
{
    public interface IVertex<T> : IEquatable<IVertex<T>>, ITraversable<T>
    {
        string Namespace { get; }
        string Id { get; }
        string Label { get; set; }
        T Attributes { get; set; }
        IReadOnlyCollection<IEdge<T>> Edges { get; }

        IEdge<T> Connect(string vertexId, string edgeLabel = null, Direction direction = Direction.Out, double weight = 0);

        Task SaveAsync(CancellationToken cancellation = default);
    }
}