using NSubstitute;
using RedisTribute.Serialization;
using RedisTribute.Serialization.CustomSerializers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Xunit;

namespace RedisTribute.UnitTests.Serialization.CustomSerializers
{
    public class XDocumentSerializerTests
    {
        readonly IObjectWriter _writer;
        readonly IObjectReader _reader;
        readonly IList<byte[]> _data;

        readonly XDocumentSerializer _serializer = new XDocumentSerializer();
        readonly XDocument _doc = new XDocument(new XElement("root", new XAttribute("x", "y"), new XComment("test")));

        public XDocumentSerializerTests()
        {
            _writer = Substitute.For<IObjectWriter>();
            _reader = Substitute.For<IObjectReader>();
            _data = new List<byte[]>();

            _writer.When(x => x.Raw(Arg.Any<byte[]>(), Arg.Any<int?>()))
                .Do(call => { _data.Add(call.Arg<byte[]>()); });

            _reader.Raw().Returns(call => new MemoryStream(_data.Single()));
        }

        [Fact]
        public void WriteData_XDocumentEntireDoc_CanReadSameData()
        {
            _serializer.WriteData(_doc, _writer);

            var doc2 = ((IObjectSerializer<XDocument>) _serializer).ReadData(_reader, null);

            Assert.Equal("root", doc2.Root.Name.LocalName);
        }

        [Fact]
        public void WriteData_XNodeEntireDoc_CanReadSameData()
        {
            _serializer.WriteData((XNode)_doc, _writer);

            var node = ((IObjectSerializer<XNode>)_serializer).ReadData(_reader, null);
            var doc2 = node as XDocument;

            Assert.Equal("root", doc2.Root.Name.LocalName);
        }
    }
}
