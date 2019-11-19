using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.Types.Graphs
{
    public static class GraphConnectExtensions
    {
        public static async Task<Edge<T>> ConnectToAndSaveAsync<T>(this IVertex<T> from, IVertex<T> to, double fromWeight = 1, double? toWeight = null, CancellationToken cancellation = default)
        {
            var edge = from.Connect(to.Label, fromWeight);

            if (toWeight.HasValue)
            {
                to.Connect(from.Label, toWeight.Value);
            }

            await from.SaveAsync(cancellation);
            await to.SaveAsync(cancellation);

            return edge;
        }
    }
}