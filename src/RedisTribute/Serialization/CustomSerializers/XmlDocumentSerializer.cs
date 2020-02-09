using System.IO;
using System.Xml;

namespace RedisTribute.Serialization.CustomSerializers
{
    class XmlDocumentSerializer : IObjectSerializer<XmlDocument>, IObjectSerializer<XmlElement>
    {
        public XmlDocument ReadData(IObjectReader reader, XmlDocument defaultValue)
        {
            var doc = new XmlDocument();

            using (var data = reader.Raw())
            {
                doc.Load(data);
            }

            return doc;
        }

        public XmlElement ReadData(IObjectReader reader, XmlElement defaultValue)
        {
            var doc = new XmlDocument();

            using (var data = reader.Raw())
            {
                doc.Load(data);
            }

            return doc.DocumentElement;
        }

        public void WriteData(XmlDocument instance, IObjectWriter writer)
        {
            using (var ms = new MemoryStream())
            using (var xmlWriter = XmlWriter.Create(ms))
            {
                instance.WriteTo(xmlWriter);
                writer.Raw(ms.ToArray());
            }
        }

        public void WriteData(XmlElement instance, IObjectWriter writer)
        {
            using (var ms = new MemoryStream())
            using (var xmlWriter = XmlWriter.Create(ms))
            {
                instance.WriteTo(xmlWriter);
                writer.Raw(ms.ToArray());
            }
        }
    }
}