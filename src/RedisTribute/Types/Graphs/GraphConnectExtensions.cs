using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.Types.Graphs
{
    public static class GraphConnectExtensions
    {
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