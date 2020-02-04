using System;
using System.Linq.Expressions;

namespace RedisTribute.Types.Graphs
{
    public interface IQueryBuilder<T>
    {
        IQuery<T> Build();
        IQueryBuilder<T> HasAttributes(Expression<Func<T, bool>> attributeFilter);
        IQueryBuilder<T> HasLabel(Expression<Func<string, bool>> labelFilter);
        IQueryBuilder<T> HasLabel(string label);
        IQueryBuilder<T> In(Expression<Func<IEdge, bool>> edgeFilter);
        IQueryBuilder<T> In(string edgeLabel);
        IQueryBuilder<T> Out(Expression<Func<IEdge, bool>> edgeFilter);
        IQueryBuilder<T> Out(string edgeLabel);
    }
}