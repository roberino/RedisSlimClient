using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.Types.Graphs
{
    public static class QueryExtensions
    {
        public static ITraversable<T> ApplyQuery<T>(this ITraversable<T> vertex, IQueryBuilder<T> query)
        {
            var queryVisitor = new QueryVisitor<T>(query.Build());
            var visitor = new Traversal<T>(vertex, queryVisitor);

            return visitor;
        }

        public static ITraversable<T> Filter<T>(this ITraversable<T> vertex, Action<IQueryBuilder<T>> querySpecification)
        {
            var query = Query<T>.Create();

            querySpecification.Invoke(query);

            var queryVisitor = new QueryVisitor<T>(query.Build());
            var visitor = new Traversal<T>(vertex, queryVisitor);

            return visitor;
        }

        public static async Task<IReadOnlyCollection<IVertex<T>>> ExecuteAsync<T>(this IVertex<T> vertex, IQuery<T> query, CancellationToken cancellation = default)
        {
            var visitor = new ResultsVisitor<T>(query);

            await vertex.TraverseAsync(visitor, cancellation);

            return visitor.Results;
        }

        public static async Task<IReadOnlyCollection<IVertex<T>>> ExecuteAsync<T>(this IVertex<T> vertex, Action<IQueryBuilder<T>> querySpecification, CancellationToken cancellation = default)
        {
            var query = Query<T>.Create();

            querySpecification.Invoke(query);

            var visitor = new ResultsVisitor<T>(query.Build());

            await vertex.TraverseAsync(visitor, cancellation);

            return visitor.Results;
        }
    }
}
