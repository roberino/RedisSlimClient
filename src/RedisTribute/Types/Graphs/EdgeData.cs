namespace RedisTribute.Types.Graphs
{
    public sealed class EdgeData
    {
        public string Id { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public double Weight { get; set; }
        public Direction Direction { get; set; }
        public string TargetVertexId { get; set; } = string.Empty;
    }
}
