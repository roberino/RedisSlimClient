using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.Types.Graphs
{
    class Edge<T> : IEdge<T>
    {
        public Edge(Uri location, EdgeData data, Func<CancellationToken, Task<IVertex<T>>> vertexLink, bool isNew = false)
        {
            Data = data;
            Location = location;
            OriginalDirection = data.Direction;
            TargetVertex = new VertexReference<T>(data.TargetVertexId, vertexLink);
            Dirty = isNew;
            Redirected = isNew;
            IsNew = isNew;
        }

        internal EdgeData Data { get; }

        public Uri Location { get; }

        public string Id => Data.Id;

        public Direction OriginalDirection { get; }

        public Direction Direction
        {
            get => Data.Direction;
            set
            {
                if (Data.Direction != value)
                {
                    Data.Direction = value;
                    Dirty = true;
                    Redirected = true;
                }
            }
        }

        public VertexReference<T> TargetVertex { get; }

        public string Label
        {
            get => Data.Label;
            set
            {
                Data.Label = value;
                Dirty = true;
            }
        }

        public double Weight
        {
            get => Data.Weight;
            set
            {
                Data.Weight = value;
                Dirty = true;
            }
        }

        public bool Redirected { get; private set; }

        public bool Removed { get; private set; }

        public bool Dirty { get; private set; }

        public bool IsNew { get; private set; }

        public void Remove()
        {
            Removed = true;
            Dirty = true;
        }

        public void Clean()
        {
            Redirected = false;
            Dirty = false;
        }

        public int CompareTo(object? obj)
        {
            if (obj is Edge<T> e)
            {
                return e.Weight.CompareTo(Weight);
            }

            return -1;
        }

        public bool Equals(IEdge<T>? other)
        {
            if(other == null)
            {
                return false;
            }

            return string.Equals(other.Id, Id);
        }

        public override int GetHashCode() => Id.GetHashCode();

        public override bool Equals(object? obj) => Equals(obj as IEdge<T>);
    }
}