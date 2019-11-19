using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.Types.Graphs
{
    public interface IVertex<T> : IEquatable<IVertex<T>>
    {
        string Namespace { get; }
        string Label { get; }
        T Attributes { get; }
        IReadOnlyCollection<Edge<T>> Edges { get; }

        Edge<T> Connect(string label, double weight = 0);
        void Remove(string label);
        Task SaveAsync(CancellationToken cancellation = default);
        Task AcceptAsync(IVisitor<T> visitor, CancellationToken cancellation = default);
    }
}