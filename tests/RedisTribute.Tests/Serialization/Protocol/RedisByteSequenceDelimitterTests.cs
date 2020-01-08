using RedisTribute.Serialization.Protocol;
using System;
using System.Buffers;
using System.Linq;
using System.Text;
using Xunit;

namespace RedisTribute.UnitTests.Serialization.Protocol
{
    public class RedisByteSequenceDelimitterTests
    {
        [Theory]
        [InlineData("abc\r\n", "abc\r\n")]
        [InlineData("abc\r\nefg", "abc\r\n")]
        [InlineData("a\r\nefg", "a\r\n")]
        [InlineData("*5\r\n+efg", "*5\r\n")]
        public void Delimit_SomeString_ReturnsCorrectPosition(string data, string expected)
        {
            var delimitter = new RedisByteSequenceDelimitter();

            var bytes = BytesFromString(data);

            var next = GetNext(delimitter, bytes);

            Assert.Equal(expected, next.result);
        }

        [Fact]
        public void Delimit_NullStringFollowedByValue_ReturnsCorrectPosition()
        {
            var delimitter = new RedisByteSequenceDelimitter();

            var bytes = BytesFromString("$0\r\n$3\r\n123\r\n");

            var seg1 = GetNext(delimitter, bytes);
            var seg2 = GetNext(delimitter, seg1.remaining);
            var seg3 = GetNext(delimitter, seg2.remaining);

            Assert.Equal("$0\r\n", seg1.result);
            Assert.Equal("$3\r\n", seg2.result);
            Assert.Equal("123\r\n", seg3.result);
        }

        [Theory]
        [InlineData("$4\r\nabcd\r\n", "$4\r\n", "abcd\r\n")]
        [InlineData("$4\r\nabcd\r\n+xyz", "$4\r\n", "abcd\r\n")]
        [InlineData("$4\r\na$cd\r\n", "$4\r\n", "a$cd\r\n")]
        [InlineData("$5\r\na\r\ncd\r\n", "$5\r\n", "a\r\ncd\r\n")]
        [InlineData("$7\r\na$1\r\ncd\r\n", "$7\r\n", "a$1\r\ncd\r\n")]
        [InlineData("*2\r\n$3\r\nabc\r\n+xx\r\n", "*2\r\n", "$3\r\n")]
        public void Delimit_BulkString_ReturnsCorrectPosition(string data, string expected1, string expected2)
        {
            var delimitter = new RedisByteSequenceDelimitter();

            var bytes = BytesFromString(data);

            var next0 = GetNext(delimitter, bytes);
            var next1 = GetNext(delimitter, next0.remaining);

            Assert.Equal(expected1, next0.result);
            Assert.Equal(expected2, next1.result);
        }

        //$1801\r\n*2\r\n*4\r\n+Id

        [Fact]
        public void Delimit_BulkStringLengthNotComplete_DelimitsWhenDataAvailable()
        {
            var data0 = "$66545";
            var data1 = "$66545\r\n+ab";

            var delimitter = new RedisByteSequenceDelimitter();

            var bytes0 = BytesFromString(data0);
            var bytes1 = BytesFromString(data1);

            var next0 = GetNext(delimitter, bytes0);
            var next1 = GetNext(delimitter, bytes1);

            Assert.Null(next0.result);
            Assert.Equal("$66545\r\n", next1.result);
        }

        [Fact]
        public void Delimit_BulkStringNotComplete_DelimitsWhenDataAvailable()
        {
            var data0 = "$6\r\n?123";
            var data1 = "?12345\r\n+abc\r\n";

            var delimitter = new RedisByteSequenceDelimitter();

            var bytes0 = BytesFromString(data0);
            var bytes1 = BytesFromString(data1);

            var next0 = GetNext(delimitter, bytes0);
            var next1 = GetNext(delimitter, next0.remaining);
            var next2 = GetNext(delimitter, bytes1);
            var next3 = GetNext(delimitter, next2.remaining);

            Assert.Equal("$6\r\n", next0.result);
            Assert.Null(next1.result);
            Assert.Equal("?12345\r\n", next2.result);
            Assert.Equal("+abc\r\n", next3.result);
        }

        [Fact]
        public void Delimit_RandomLargeStrings_DelimitsForEachBreak()
        {
            var sb = new StringBuilder();

            for (var i = 0; i < 100; i++)
            {
                for (var j = 0; j < 7; j++)
                {
                    sb.Append(Guid.NewGuid());

                    if (j * i % 3 == 0)
                    {
                        sb.Append("x\r\n");
                    }
                }
            }

            var delimitter = new RedisByteSequenceDelimitter();

            var bytes = BytesFromString(sb.ToString());

            var next = GetNext(delimitter, bytes);

            while (next.result != null)
            {
                Assert.Equal('\n', next.result.Last());
                next = GetNext(delimitter, next.remaining);
            }
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