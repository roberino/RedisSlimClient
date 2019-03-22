using RedisSlimClient.Io;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace RedisSlimClient.Tests.Io
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
    }
}