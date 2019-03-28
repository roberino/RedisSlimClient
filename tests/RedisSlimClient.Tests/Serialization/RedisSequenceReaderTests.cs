using System.IO;
using System.Linq;
using System.Text;
using RedisSlimClient.Io;
using RedisSlimClient.Serialization;
using RedisSlimClient.Types;
using Xunit;

namespace RedisSlimClient.Tests.Serialization
{
    public class RedisSequenceReaderTests
    {
        [Fact]
        public void Read_SimpleString()
        {
            var reader = GetReader("+hello\r\n");

            var parsedObject = (RedisString)reader.ToObjects().Single();
            var value = parsedObject.ToString();

            Assert.Equal("hello", value);
            Assert.Equal(RedisType.String, parsedObject.Type);
        }

        [Fact]
        public void Read_Array_ReturnsExpectedMembers()
        {
            var reader = GetReader("*3\r\n:1234\r\n+hi\r\n-me-error\r\n");

            var parsedObject = (RedisArray)reader.ToObjects().Single();

            Assert.Equal(3, parsedObject.Count);
            Assert.Equal(RedisType.Array, parsedObject.Type);

            var int1 = (RedisInteger)parsedObject.ElementAt(0);
            var str1 = (RedisString)parsedObject.ElementAt(1);
            var err1 = (RedisError)parsedObject.ElementAt(2);

            Assert.Equal(1234, int1.Value);
            Assert.Equal("hi", str1.ToString());
            Assert.Equal("me-error", err1.Message);
        }

        [Fact]
        public void Read_NestedArray_ReturnsExpectedMembers()
        {
            var reader = GetReader("*3\r\n+abc\r\n*2\r\n:123\r\n:456\r\n+efg\r\n");

            var parsedObjects = reader.ToObjects().ToList();

            Assert.Equal(2, parsedObjects.Count);

            var arr1 = (RedisArray)parsedObjects[0];
            var str1 = (RedisString)arr1.Items[0];
            var arr2 = (RedisArray) arr1.Items[1];
            var int1 = (RedisInteger) arr2.Items[0];
            var int2 = (RedisInteger)arr2.Items[1];
            var str2 = (RedisString)parsedObjects[1];

            Assert.Equal("abc", str1.ToString());
            Assert.Equal(123, int1.Value);
            Assert.Equal(456, int2.Value);
            Assert.Equal("efg", str2.ToString());
        }

        [Theory]
        [InlineData("hello")]
        [InlineData("abcdefghijklmnopqrstuvwxzy")]
        [InlineData("x-y-z")]
        [InlineData("1234")]
        [InlineData("<??>")]
        public void Read_BulkStringWithAsciiChars_ReturnsCorrectOutput(string str)
        {
            var reader = GetReader($"${str.Length}\r\n{str}\r\n");

            var parsedObject = (RedisString)reader.ToObjects().Single();
            var value = parsedObject.ToString();

            Assert.Equal(str, value);
            Assert.Equal(RedisType.String, parsedObject.Type);
        }

        [Fact]
        public void Read_MultipleStrings_ReturnsCorrectOutput()
        {
            var reader = GetReader("+abcd\r\n$4\r\nefgh\r\n$4\r\nijkl\r\n");

            var parsedObjects = reader.ToObjects().ToArray();
            var value1 = parsedObjects[0].ToString();
            var value2 = parsedObjects[1].ToString();
            var value3 = parsedObjects[2].ToString();

            Assert.Equal("abcd", value1);
            Assert.Equal("efgh", value2);
            Assert.Equal("ijkl", value3);
        }

        [Fact]
        public void Read_Integer()
        {
            var reader = GetReader(":12345678\r\n");

            var parsedObject = (RedisInteger)reader.ToObjects().Single();
            var value = parsedObject.Value;

            Assert.Equal(12345678, value);
            Assert.Equal(RedisType.Integer, parsedObject.Type);
        }

        static RedisSequenceReader GetReader(string data)
        {
            var stream = new MemoryStream(GetData(data));

            return new RedisSequenceReader(new StreamIterator(stream));
        }

        static byte[] GetData(string value) => Encoding.ASCII.GetBytes(value);
    }
}
