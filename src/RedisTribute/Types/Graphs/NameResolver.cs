using System;
using System.Linq;

namespace RedisTribute.Types.Graphs
{
    enum GraphObjectType
    {
        None = 0,
        Metadata,
        Label,
        Edge,
        Vertex
    }

    class NameResolver
    {
        public NameResolver(string graphNamespace)
        {
            BaseUri = new Uri($"graph://{graphNamespace}/");
        }

        public string Namespace => BaseUri.Host;

        public Uri BaseUri { get; }

        public void ValidateId(string id)
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }
            if (id.Length == 0)
            {
                throw new ArgumentException(nameof(id));
            }
            if (!char.IsLetter(id[0]))
            {
                throw new ArgumentException($"{nameof(id)} must start with a letter");
            }
            if (id.Any(c => !char.IsLetterOrDigit(c) || c == '_' || c == '-'))
            {
                throw new ArgumentException($"{nameof(id)} must only contain chars [a-z_-]");
            }
        }

        public bool IsType(string localPath, GraphObjectType type) => GetType(localPath) == type;

        public GraphObjectType GetType(string localPath) => GetType(new Uri(BaseUri, localPath));

        public GraphObjectType GetType(Uri location)
        {
            var part = location.PathAndQuery.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).First();

            Enum.TryParse<GraphObjectType>(part, true, out var type);

            return type;
        }

        public Uri GetLocation(GraphObjectType graphObjectType, string id) => new Uri(BaseUri, $"/{graphObjectType.ToString().ToLower()}/{Uri.EscapeDataString(id)}");

        public string GetValue(string relPath)
        {
            var uri = new Uri(BaseUri, relPath);

            var parts = uri.PathAndQuery.Split('/', '?');

            if (parts.Length > 1)
            {
                return Uri.UnescapeDataString(parts[1]);
            }

            return null;
        }
    }
}