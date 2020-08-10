using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace RedisTribute.Serialization.CustomSerializers
{
    class XDocumentSerializer : 
        IObjectSerializer<XDocument>, 
        IObjectSerializer<XElement>, 
        IObjectSerializer<XNode>
    {
        readonly XmlReaderSettings _settings = new XmlReaderSettings()
        {
            ConformanceLevel = ConformanceLevel.Fragment
        };

        public XDocument ReadData(IObjectReader reader, XDocument defaultValue)
        {
            using (var data = reader.Raw())
            using (var textReader = new StreamReader(data, Encoding.UTF8))
            {
                return XDocument.Load(textReader);
            }
        }

        public XElement ReadData(IObjectReader reader, XElement defaultValue)
        {
            using (var data = reader.Raw())
            using (var textReader = new StreamReader(data, Encoding.UTF8))
            {
                return XElement.Load(textReader);
            }
        }

        public XNode ReadData(IObjectReader reader, XNode defaultValue)
        {
            using (var data = reader.Raw())
            using (var textReader = new StreamReader(data, Encoding.UTF8))
            using (var xmlReader = XmlReader.Create(textReader, _settings))
            {
                xmlReader.MoveToContent();
                return XNode.ReadFrom(xmlReader);
            }
        }

        public void WriteData(XDocument instance, IObjectWriter writer) => WriteData(((XNode)instance), writer);

        public void WriteData(XElement instance, IObjectWriter writer) => WriteData(((XNode)instance), writer);

        public void WriteData(XNode instance, IObjectWriter writer)
        {
            using (var ms = new MemoryStream())
            using (var xmlWriter = XmlWriter.Create(ms, new XmlWriterSettings()
            {
                Encoding = Encoding.UTF8,
                Indent = false
            }))
            {
                instance.WriteTo(xmlWriter);
                xmlWriter.Flush();
                ms.Flush();
                writer.Raw(ms.ToArray());
            }
        }
    }
}