using System.IO;
using System.Xml;

namespace RedisTribute.Serialization.CustomSerializers
{
    class XmlDocumentSerializer : IObjectSerializer<XmlDocument>
    {
        const string ItemName = "xml";

        public XmlDocument ReadData(IObjectReader reader, XmlDocument defaultValue)
        {
            var data = reader.ReadBytes(ItemName);

            var doc = new XmlDocument();

            using (var ms = new MemoryStream(data))
            {
                doc.Load(ms);
            }

            return doc;
        }

        public void WriteData(XmlDocument instance, IObjectWriter writer)
        {
            using (var ms = new MemoryStream())
            using (var xmlWriter = XmlWriter.Create(ms))
            {
                instance.WriteTo(xmlWriter);
                writer.WriteItem(ItemName, ms.ToArray());
            }
        }
    }
}