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

        Task<bool> MatchesEdgeAsync(IEdge edge);
    }

    public class Query<T> : IQueryBuilder<T>
    {
        readonly List<Expression<Func<string, bool>>> _labelFilters;
        readonly List<Expression<Func<T, bool>>> _attributeFilters;
        readonly List<Expression<Func<IEdge, bool>>> _edgeFilters;

        Query()
        {
            _labelFilters = new List<Expression<Func<string, bool>>>();
            _attributeFilters = new List<Expression<Func<T, bool>>>();
            _edgeFilters = new List<Expression<Func<IEdge, bool>>>();
        }

        public static IQueryBuilder<T> Create()
        {
            return new Query<T>();
        }

        public IQueryBuilder<T> HasLabel(Expression<Func<string, bool>> labelFilter)
        {
            _labelFilters.Add(labelFilter);
            return this;
        }

        public IQueryBuilder<T> HasLabel(string label)
        {
            return HasLabel(x => string.Equals(x, label));
        }

        public IQueryBuilder<T> HasAttributes(Expression<Func<T, bool>> attributeFilter)
        {
            _attributeFilters.Add(attributeFilter);
            return this;
        }

        public IQueryBuilder<T> Out(Expression<Func<IEdge, bool>> edgeFilter)
        {
            _edgeFilters.Add(ConjunctiveJoin(e => e.Direction == Direction.Out || e.Direction == Direction.Bidirectional, edgeFilter));
            return this;
        }

        public IQueryBuilder<T> In(Expression<Func<IEdge, bool>> edgeFilter)
        {
            _edgeFilters.Add(ConjunctiveJoin(e => e.Direction == Direction.In || e.Direction == Direction.Bidirectional, edgeFilter));
            return this;
        }

        public IQueryBuilder<T> Out(string edgeLabel) => Out(e => string.Equals(e.Label, edgeLabel));

        public IQueryBuilder<T> In(string edgeLabel) => In(e => string.Equals(e.Label, edgeLabel));

        public IQuery<T> Build()
        {
            var labelCondition = CreateCondition(_labelFilters);
            var attributeCondition = CreateCondition(_attributeFilters);
            var edgeCondition = CreateCondition(_edgeFilters);

            return new QueryImpl(Create(labelCondition, attributeCondition), edgeCondition);
        }

        static Func<TInput, bool> CreateCondition<TInput>(IList<Expression<Func<TInput, bool>>> conditions)
        {
            if (conditions.Count > 0)
            {
                var e = conditions[0].Body;
                var parameters = conditions[0].Parameters;

                foreach (var condition in conditions.Skip(1))
                {
                    e = Expression.And(e, UpdateParameter(condition, parameters[0]).Body);
                }

                return Expression.Lambda(e, parameters).Compile() as Func<TInput, bool>;
            }

            return null;
        }

        static Expression<Func<TInput, bool>> ConjunctiveJoin<TInput>(Expression<Func<TInput, bool>> condition1, Expression<Func<TInput, bool>> condition2)
        {
            var e = condition1.Body;
            var parameters = condition1.Parameters;

            e = Expression.And(e, UpdateParameter(condition2, parameters[0]).Body);

            return Expression.Lambda(e, parameters) as Expression<Func<TInput, bool>>;
        }

        static Expression<Func<TParam, bool>> UpdateParameter<TParam>(
            Expression<Func<TParam, bool>> expr,
            ParameterExpression newParameter)
        {
            var visitor = new ParameterUpdateVisitor(expr.Parameters[0], newParameter);
            var body = visitor.Visit(expr.Body);

            return Expression.Lambda<Func<TParam, bool>>(body, newParameter);
        }

        static Func<IVertex<T>, Task<bool>> Create(Func<string, bool> labelFilter, Func<T, bool> attributeFilter)
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

                await Task.CompletedTask;

                return true;
            };
        }

        class QueryImpl : IQuery<T>
        {
            readonly Func<IVertex<T>, Task<bool>> _condition;
            private readonly Func<IEdge, bool> _edgeCondition;

            public QueryImpl(Func<IVertex<T>, Task<bool>> condition, Func<IEdge, bool> edgeCondition)
            {
                _condition = condition;
                _edgeCondition = edgeCondition;
            }

            public Task<bool> ExecuteAsync(IVertex<T> vertex)
            {
                return _condition(vertex);
            }

            public Task<bool> MatchesEdgeAsync(IEdge edge)
            {
                if (_edgeCondition == null)
                {
                    return Task.FromResult(true);
                }

                return Task.FromResult(_edgeCondition(edge));
            }
        }

        class ParameterUpdateVisitor : ExpressionVisitor
        {
            ParameterExpression _oldParameter;
            ParameterExpression _newParameter;

            public ParameterUpdateVisitor(ParameterExpression oldParameter, ParameterExpression newParameter)
            {
                _oldParameter = oldParameter;
                _newParameter = newParameter;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                if (ReferenceEquals(node, _oldParameter))
                    return _newParameter;

                return base.VisitParameter(node);
            }
        }
    }
}