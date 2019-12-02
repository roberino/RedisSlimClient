using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.Types.Graphs
{
    public interface IVisitor<T>
    {
        Task<bool> VisitAsync(IVertex<T> vertex, CancellationToken cancellation);

        Task<bool> ShouldTraverseAsync(IEdge<T> edge, CancellationToken cancellation);
    }
}
