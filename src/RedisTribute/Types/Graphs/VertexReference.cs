using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.Types.Graphs
{
    public readonly struct VertexReference<T>
    {
        private readonly Func<CancellationToken, Task<IVertex<T>>> _vertexLink;

        public VertexReference(string vertexId, Func<CancellationToken, Task<IVertex<T>>> vertexLink)
        {
            Id = vertexId;
            _vertexLink = vertexLink;
        }

        public string Id { get; }

        public Task<IVertex<T>> GetVertex(CancellationToken cancellation) => _vertexLink(cancellation);
    }
}