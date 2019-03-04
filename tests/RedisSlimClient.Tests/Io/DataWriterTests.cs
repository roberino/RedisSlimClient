using System;
using System.IO;
using System.Text;
using RedisSlimClient.Io;
using Xunit;

namespace RedisSlimClient.Tests.Io
{
    public class DataWriterTests : IDisposable
    {
        readonly MemoryStream _output;

        public DataWriterTests()
        {
            _output = new MemoryStream();
        }

        [Fact]
        public void Write_SomeStringDataBulk_WritesExpectedBytes()
        {
            _output.Write("some-data", true);

            AssertExpectedString("$9\r\nsome-data\r\n");
        }

        [Fact]
        public void Write_SomeStringDataSimple_WritesExpectedBytes()
        {
            _output.Write("some-data");

            AssertExpectedString("+some-data\r\n");
        }

        void AssertExpectedString(string expected)
        {
            var actual = Encoding.ASCII.GetString(_output.ToArray());

            Assert.Equal(expected, actual);
        }

        public void Dispose()
        {
            _output?.Dispose();
        }
    }
}