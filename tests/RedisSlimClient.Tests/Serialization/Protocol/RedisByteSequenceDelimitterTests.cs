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

            var next = GetNext(delimitter, bytes);

            Assert.Equal(expected, next.result);
        }

        [Theory]
        [InlineData("$4\r\nabcd\r\n", "$4\r\n", "abcd\r\n")]
        [InlineData("$4\r\nabcd\r\n+xyz", "$4\r\n", "abcd\r\n")]
        [InlineData("$4\r\na$cd\r\n", "$4\r\n", "a$cd\r\n")]
        [InlineData("$5\r\na\r\ncd\r\n", "$5\r\n", "a\r\ncd\r\n")]
        [InlineData("$7\r\na$1\r\ncd\r\n", "$7\r\n", "a$1\r\ncd\r\n")]
        public void Delimit_BulkString_ReturnsCorrectPosition(string data, string expected1, string expected2)
        {
            var delimitter = new RedisByteSequenceDelimitter();

            var bytes = BytesFromString(data);

            var next0 = GetNext(delimitter, bytes);
            var next1 = GetNext(delimitter, next0.remaining);

            Assert.Equal(expected1, next0.result);
            Assert.Equal(expected2, next1.result);
        }

        (string result, ReadOnlySequence<byte> remaining) GetNext(RedisByteSequenceDelimitter delimitter, ReadOnlySequence<byte> bytes)
        {
            var pos = delimitter.Delimit(bytes);

            if (!pos.HasValue)
            {
                return (null, default);
            }

            var posIncDelimitter = bytes.GetPosition(1, pos.Value);
            var span = bytes.Slice(0, posIncDelimitter);
            var arr = span.First.ToArray();
            var rem = bytes.Slice(posIncDelimitter);

            return (Encoding.ASCII.GetString(arr), rem);
        }

        ReadOnlySequence<byte> BytesFromString(string data) => new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes(data));
    }
}