using RedisSlimClient.Serialization.Protocol;
using System.Buffers;
using System.Text;
using Xunit;

namespace RedisSlimClient.UnitTests.Serialization.Protocol
{
    public class RedisByteSequenceDelimitterTests
    {
        [Theory]
        [InlineData("abc\r\n", "abc\r\n")]
        [InlineData("abc\r\nefg", "abc\r\n")]
        [InlineData("a\r\nefg", "a\r\n")]
        public void Delimit_SomeString_ReturnsCorrectPosition(string data, string expected)
        {
            var delimitter = new RedisByteSequenceDelimitter();

            var bytes = BytesFromString(data);
            var pos = delimitter.Delimit(bytes);

            var adjusted = bytes.GetPosition(1, pos.Value);

            var span = bytes.Slice(0, adjusted);

            Assert.Equal(expected.Length, span.Length);

            var arr = span.First.ToArray();

            var result = Encoding.ASCII.GetString(arr);

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("$4\r\nabcd\r\n", "$4\r\nabcd\r\n")]
        [InlineData("$4\r\nabcd\r\n+xyz", "$4\r\nabcd\r\n")]
        [InlineData("$4\r\na$cd\r\n", "$4\r\na$cd\r\n")]
        [InlineData("$5\r\na\r\ncd\r\n", "$5\r\na\r\ncd\r\n")]
        [InlineData("$7\r\na$1\r\ncd\r\n", "$7\r\na$1\r\ncd\r\n")]
        public void Delimit_BulkString_ReturnsCorrectPosition(string data, string expected)
        {
            var delimitter = new RedisByteSequenceDelimitter();

            var bytes = BytesFromString(data);
            var pos = delimitter.Delimit(bytes);

            var span = bytes.Slice(0, bytes.GetPosition(1, pos.Value));

            Assert.Equal(expected.Length, span.Length);

            var arr = span.First.ToArray();

            var result = Encoding.ASCII.GetString(arr);

            Assert.Equal(expected, result);
        }

        ReadOnlySequence<byte> BytesFromString(string data) => new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes(data));
    }
}