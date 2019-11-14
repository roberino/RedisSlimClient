using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.Types.Graphs
{
    public interface IVertex<T>
    {
        string Label { get; }
        T Attributes { get; }
        IReadOnlyCollection<Edge<T>> Edges { get; }
        Edge<T> Connect(string label, double weight = 0);
        void Remove(string label);
        Task SaveAsync(CancellationToken cancellation = default);
    }
}