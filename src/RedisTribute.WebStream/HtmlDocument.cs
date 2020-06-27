using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace RedisTribute.WebStream
{
    public sealed class HtmlDocument
    {
        public Uri DocumentUri { get; set; }

        public XDocument Content { get; set; }

        public IEnumerable<Uri> Getlinks()
        {
            if (Content == null || DocumentUri == null || Content.Root == null)
            {
                return Enumerable.Empty<Uri>();
            }

            return FindLinks(Content.Root.Elements()).Select(x => new Uri(DocumentUri, x));
        } 

        static IEnumerable<string> FindLinks(IEnumerable<XNode> selected)
        {
            return selected
                .Where(x => x != null && x.NodeType == System.Xml.XmlNodeType.Element)
                .Cast<XElement>()
                .SelectMany(GetLinks)
                .Distinct();
        }

        static IEnumerable<string> GetLinks(XElement e)
        {
            return
                e.Descendants().Where(x => x.Name.LocalName == "a").Select(a => a.Attribute("href"))
                    .Where(a => a != null).Select(a => a.Value);
        }
    }
}
