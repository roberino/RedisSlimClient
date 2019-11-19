using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace RedisTribute.Types.Graphs
{
    public interface IQuery<T>
    {
        Task<bool> ExecuteAsync(IVertex<T> vertex);

        Task<bool> MatchesEdgeAsync(Edge<T> edge);
    }

    public class Query<T>
    {
        readonly List<Expression<Func<string, bool>>> _labelFilters;
        readonly List<Expression<Func<T, bool>>> _attributeFilters;
        readonly List<Expression<Func<Edge<T>, bool>>> _edgeFilters;

        Query()
        {
            _labelFilters = new List<Expression<Func<string, bool>>>();
            _attributeFilters = new List<Expression<Func<T, bool>>>();
            _edgeFilters = new List<Expression<Func<Edge<T>, bool>>>();
        }

        public static Query<T> Create()
        {
            return new Query<T>();
        }

        public Query<T> WithLabel(Expression<Func<string, bool>> labelFilter)
        {
            _labelFilters.Add(labelFilter);
            return this;
        }

        public Query<T> WithLabel(string label)
        {
            return WithLabel(x => string.Equals(x, label));
        }

        public Query<T> WithAttributes(Expression<Func<T, bool>> attributeFilter)
        {
            _attributeFilters.Add(attributeFilter);
            return this;
        }

        public Query<T> WithEdges(Expression<Func<Edge<T>, bool>> edgeFilter)
        {
            _edgeFilters.Add(edgeFilter);
            return this;
        }

        public IQuery<T> Build()
        {
            var labelCondition = CreateCondition(_labelFilters);
            var attributeCondition = CreateCondition(_attributeFilters);
            var edgeCondition = CreateCondition(_edgeFilters);

            return new QueryImpl(Create(labelCondition, attributeCondition, edgeCondition), edgeCondition);
        }

        Func<TInput, bool> CreateCondition<TInput>(IList<Expression<Func<TInput, bool>>> conditions)
        {
            if (conditions.Count > 0)
            {
                var e = conditions[0].Body;

                foreach (var label in conditions.Skip(1))
                {
                    e = Expression.And(e, label.Body);
                }

                return Expression.Lambda(e, conditions[0].Parameters).Compile() as Func<TInput, bool>;
            }

            return null;
        }

        Func<IVertex<T>, Task<bool>> Create(Func<string, bool> labelFilter, Func<T, bool> attributeFilter, Func<Edge<T>, bool> edgeFilter)
        {
            return async x =>
            {
                if (labelFilter != null)
                {
                    if (!labelFilter.Invoke(x.Label))
                    {
                        return false;
                    }
                }

                if (attributeFilter != null)
                {
                    if (!attributeFilter(x.Attributes))
                    {
                        return false;
                    }
                }

                if (edgeFilter != null)
                {
                    return x.Edges.Any(e => edgeFilter(e));
                }

                await Task.CompletedTask;

                return true;
            };
        }

        private class QueryImpl : IQuery<T>
        {
            readonly Func<IVertex<T>, Task<bool>> _condition;
            private readonly Func<Edge<T>, bool> _edgeCondition;

            public QueryImpl(Func<IVertex<T>, Task<bool>> condition, Func<Edge<T>, bool> edgeCondition)
            {
                _condition = condition;
                _edgeCondition = edgeCondition;
            }

            public Task<bool> ExecuteAsync(IVertex<T> vertex)
            {
                return _condition(vertex);
            }

            public Task<bool> MatchesEdgeAsync(Edge<T> edge)
            {
                if (_edgeCondition == null)
                {
                    return Task.FromResult(true);
                }

                return Task.FromResult(_edgeCondition(edge));
            }
        }
    }
}