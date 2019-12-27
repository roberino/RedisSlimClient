using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.Types.Graphs
{
    public interface IGraph
    {
        Task<IVertex<T>> GetVertexAsync<T>(string id, CancellationToken cancellation = default);
    }
}