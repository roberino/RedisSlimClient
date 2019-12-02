using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.Types.Graphs
{
    public static class QueryExtensions
    {
        public static async Task<IReadOnlyCollection<IVertex<T>>> QueryAsync<T>(this IVertex<T> vertex, IQuery<T> query, CancellationToken cancellation = default)
        {
            var visitor = new QueryVisitor<T>(query);

            await vertex.TraverseAsync(visitor, cancellation);

            return visitor.Results;
        }

        class QueryVisitor<T> : IVisitor<T>
        {
            readonly IQuery<T> _query;
            readonly ConcurrentDictionary<string, IVertex<T>> _results;

            public QueryVisitor(IQuery<T> query)
            {
                _query = query;
                _results = new ConcurrentDictionary<string, IVertex<T>>();
            }

            public async Task<bool> VisitAsync(IVertex<T> vertex, CancellationToken cancellation)
            {
                if (await _query.ExecuteAsync(vertex))
                {
                    if (_results.TryAdd(vertex.Id, vertex))
                    {
                        return true;
                    }
                }

                return false;
            }

            public Task<bool> ShouldTraverseAsync(IEdge<T> edge, CancellationToken cancellation)
            {
                if (cancellation.IsCancellationRequested)
                {
                    return Task.FromResult(false);
                }

                return _query.MatchesEdgeAsync(edge);
            }

            public IReadOnlyCollection<IVertex<T>> Results => new List<IVertex<T>>(_results.Values);
        }
    }
}
