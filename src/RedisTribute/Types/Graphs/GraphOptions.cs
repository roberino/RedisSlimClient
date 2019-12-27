namespace RedisTribute.Types.Graphs
{
    public readonly struct GraphOptions
    {
        readonly string _namespace;

        public GraphOptions(string graphNamespace)
        {
            _namespace = graphNamespace;
        }

        public string Namespace => _namespace ?? "default";
    }
}