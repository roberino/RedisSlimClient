using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.Types.Graphs
{
    public interface ITraversable<T>
    {
        Task TraverseAsync(IVisitor<T> visitor, CancellationToken cancellation = default);
    }
}
