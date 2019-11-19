namespace RedisTribute.Types.Graphs
{
    public readonly struct GraphOptions
    {
        public GraphOptions(string graphNamespace)
        {
            Namespace = graphNamespace;
        }

        public string Namespace { get; }

        public string GetKey(string label)
        {
            if (string.IsNullOrEmpty(Namespace))
            {
                return label;
            }

            return $"{Namespace}:{label}";
        }
    }
}