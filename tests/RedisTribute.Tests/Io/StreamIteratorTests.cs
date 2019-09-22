using RedisTribute.Io;
using System;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace RedisTribute.UnitTests.Io
{
    public class StreamIteratorTests
    {
        [Fact]
        public void Iterate_SplitString_ReturnsExpectedItems()
        {
            var stream = new MemoryStream();
            var iterator = new StreamIterator(stream);

            var data = Encoding.ASCII.GetBytes("hello\r\nworld\r\n");

            stream.Write(data);
            stream.Position = 0;

            var items = iterator
                .Select(s => Encoding.ASCII.GetString(s)).ToArray();

            Assert.Equal(2, items.Length);
            Assert.Equal("hello", items[0]);
            Assert.Equal("world", items[1]);
        }

        [Fact]
        public void Iterate_LargeStream_ReturnsExpectedItems()
        {
            var stream = new MemoryStream();
            var iterator = new StreamIterator(stream);

            var sb = new StringBuilder();

            for (var i = 0; i < 500; i++)
            {
                sb.Append($"{Guid.NewGuid()}\r\n");
            }

            var data = Encoding.ASCII.GetBytes(sb.ToString());

            stream.Write(data);
            stream.Position = 0;

            var items = iterator
                .Select(s => Encoding.ASCII.GetString(s)).ToArray();

            Assert.Equal(500, items.Length);
        }

        [Fact]
        public void Iterate_StringWithNewLine_ReturnsExpectedItems()
        {
            var stream = new MemoryStream();
            var iterator = new StreamIterator(stream);

            var data = Encoding.ASCII.GetBytes("hello\nworld\r\n");

            stream.Write(data);
            stream.Position = 0;

            var items = iterator
                .Select(s => Encoding.ASCII.GetString(s)).ToArray();

            Assert.Single(items);
            Assert.Equal("hello\nworld", items[0]);
        }

        [Fact]
        public void Iterate_BufferOverflow_ReturnsExpectedItems()
        {
            var stream = new MemoryStream();
            var iterator = new StreamIterator(stream, 4);

            var data = Encoding.ASCII.GetBytes("+abcd\r\n$4\r\nefgh\r\n$4\r\nijkl\r\n");

            stream.Write(data);
            stream.Position = 0;

            var items = iterator
                .Select(s => Encoding.ASCII.GetString(s)).ToArray();

            Assert.Equal(5, items.Length);
            Assert.Equal("+abcd", items[0]);
            Assert.Equal("$4", items[1]);
            Assert.Equal("efgh", items[2]);
            Assert.Equal("$4", items[3]);
            Assert.Equal("ijkl", items[4]);
        }

        [Fact]
        public void Iterate_BulkStringContainingDelimittingChars_ReturnsExpectedItems()
        {
            var stream = new MemoryStream();
            var iterator = new StreamIterator(stream);

            var data = Encoding.ASCII.GetBytes("+abcd\r\n$6\r\nef\r\ngh\r\n+hi\r\n");

            stream.Write(data);
            stream.Position = 0;

            var items = iterator
                .Select(s => Encoding.ASCII.GetString(s)).ToArray();

            Assert.Equal(4, items.Length);
            Assert.Equal("+abcd", items[0]);
            Assert.Equal("$6", items[1]);
            Assert.Equal("ef\r\ngh", items[2]);
            Assert.Equal("+hi", items[3]);
        }

        [Fact]
        public void Iterate_BulkStringContainingBulkStringIdentifier_ReturnsExpectedItems()
        {
            var stream = new MemoryStream();
            var iterator = new StreamIterator(stream);

            var data = Encoding.ASCII.GetBytes("$4\r\n$\0\0\0\r\n");

            stream.Write(data);
            stream.Position = 0;

            var items = iterator
                .Select(s => Encoding.ASCII.GetString(s)).ToArray();

            Assert.Equal(2, items.Length);
        }
    }
}