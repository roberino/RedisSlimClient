using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.Types.Graphs
{
    class Traversal<T> : ITraversable<T>
    {
        readonly ITraversable<T> _traversable;
        readonly IVisitor<T> _filter;

        public Traversal(ITraversable<T> traversable, IVisitor<T> filter)
        {
            _traversable = traversable;
            _filter = filter;
        }

        public Task TraverseAsync(IVisitor<T> visitor, CancellationToken cancellation = default)
        {
            var chain = new VisitorChain(_filter, visitor);

            return _traversable.TraverseAsync(chain, cancellation);
        }

        class VisitorChain : IVisitor<T>
        {
            readonly IVisitor<T> _filter;
            readonly IVisitor<T> _next;

            public VisitorChain(IVisitor<T> filter, IVisitor<T> next) {
                _filter = filter;
                _next = next;
            }

            public async Task<bool> ShouldTraverseAsync(IEdge<T> edge, CancellationToken cancellation)
            {
                if (await _filter.ShouldTraverseAsync(edge, cancellation))
                {
                    return await _next.ShouldTraverseAsync(edge, cancellation);
                }

                return false;
            }

            public async Task<bool> VisitAsync(IVertex<T> vertex, CancellationToken cancellation)
            {
                if (await _filter.VisitAsync(vertex, cancellation))
                {
                    return await _next.VisitAsync(vertex, cancellation);
                }

                return false;
            }
        }
    }
}
