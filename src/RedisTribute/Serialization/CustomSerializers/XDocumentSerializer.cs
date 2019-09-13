using System.Xml.Linq;

namespace RedisTribute.Serialization.CustomSerializers
{
    class XDocumentSerializer : IObjectSerializer<XDocument>
    {
        const string ItemName = "xml";

        public XDocument ReadData(IObjectReader reader, XDocument defaultValue)
        {
            var data = reader.ReadString(ItemName);

            return XDocument.Parse(data);
        }

        public void WriteData(XDocument instance, IObjectWriter writer)
        {
            writer.WriteItem(ItemName, instance.ToString(SaveOptions.DisableFormatting));
        }
    }
}