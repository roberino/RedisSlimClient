using System;
using System.IO;
using System.Text;
using RedisSlimClient.Serialization.Protocol;
using Xunit;

namespace RedisSlimClient.UnitTests.Serialization
{
    public class RedisByteFormatterTests : IDisposable
    {
        readonly MemoryStream _output;

        public RedisByteFormatterTests()
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
        public void Write_Integer_WritesExpectedBytes()
        {
            _output.Write(6382334);

            AssertExpectedString(":6382334\r\n");
        }

        [Fact]
        public void Write_Objects_WritesExpectedBytes()
        {
            _output.Write(new object[] { 6382334, "abc" });

            AssertExpectedString("*2\r\n:6382334\r\n$3\r\nabc\r\n");
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