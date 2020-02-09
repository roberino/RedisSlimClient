using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.Types.Graphs
{
    class QueryVisitor<T> : IVisitor<T>
    {
        readonly IQuery<T> _query;

        public QueryVisitor(IQuery<T> query)
        {
            _query = query;
        }

        public async Task<bool> VisitAsync(IVertex<T> vertex, CancellationToken cancellation)
        {
            if (await _query.ExecuteAsync(vertex))
            {
                OnMatch(vertex);

                return true;
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

        protected virtual void OnMatch(IVertex<T> match)
        {

        }
    }
    class ResultsVisitor<T> : QueryVisitor<T>
    {
        readonly ConcurrentDictionary<string, IVertex<T>> _results;

        public ResultsVisitor(IQuery<T> query) : base(query)
        {
            _results = new ConcurrentDictionary<string, IVertex<T>>();
        }

        protected override void OnMatch(IVertex<T> match)
        {
            if (_results.TryAdd(match.Id, match))
            {
            }
        }

        public IReadOnlyCollection<IVertex<T>> Results => new List<IVertex<T>>(_results.Values);
    }
}
