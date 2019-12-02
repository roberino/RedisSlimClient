using System;

namespace RedisTribute.Types.Graphs
{
    public interface IEdge : IComparable
    {
        string Id { get; }
        Direction Direction { get; }
        string Label { get; set; }
        double Weight { get; set; }
    }

    public interface IEdge<T> : IEdge, IEquatable<IEdge<T>>
    {
        VertexReference<T> TargetVertex { get; }

        void Remove();
    }
}