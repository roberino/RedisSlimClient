using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RedisTribute.Types.Graphs
{
    public static class GraphExtensions
    {
        public static Task<XDocument> ExportGmlAsync<T>(this ITraversable<T> traversable, CancellationToken cancellation = default)
        {
            return new GraphMlFormatter<T>().Format(traversable, cancellation);
        }

        public static Direction Compliment(this Direction direction)
        {
            switch (direction)
            {
                case Direction.In:
                    return Direction.Out;
                case Direction.Out:
                    return Direction.In;
                default:
                    return direction;
            }
        }
    }
}