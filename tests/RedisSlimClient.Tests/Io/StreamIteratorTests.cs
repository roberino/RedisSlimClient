using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using RedisSlimClient.Io;
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

            var data = Encoding.ASCII.GetBytes("hello\n\rworld\n\r");

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

            var data = Encoding.ASCII.GetBytes("hello\n\rworld\n\r");

            stream.Write(data);
            stream.Position = 0;

            var items = iterator
                .Select(s => Encoding.ASCII.GetString(s)).ToArray();

            Assert.Equal(2, items.Length);
            Assert.Equal("hello", items[0]);
            Assert.Equal("world", items[1]);
        }
    }
}