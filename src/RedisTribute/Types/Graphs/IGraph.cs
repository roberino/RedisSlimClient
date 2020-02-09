using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.Types.Graphs
{
    public interface IGraph<T>
    {
        Task<IVertex<T>> GetVertexAsync(string id, CancellationToken cancellation = default);
    }
}