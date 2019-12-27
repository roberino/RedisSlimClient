namespace RedisTribute.Types.Graphs
{
    public sealed class EdgeData
    {
        public string Id { get; set; }
        public string Label { get; set; }

        public double Weight { get; set; }

        public Direction Direction { get; set; }

        public string TargetVertexId { get; set; }
    }
}
