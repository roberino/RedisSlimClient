﻿using RedisSlimClient.Io;
using RedisSlimClient.Io.Types;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace RedisSlimClient.Tests.Io
{
    public class DataReaderTests
    {
        [Fact]
        public void Read_SimpleString()
        {
            var reader = GetReader("+hello\n\r");

            var parsedObject = (RedisString)reader.Single();
            var value = parsedObject.AsString();

            Assert.Equal("hello", value);
            Assert.Equal(RedisType.String, parsedObject.Type);
        }

        [Fact]
        public void Read_Array_ReturnsExpectedMembers()
        {
            var reader = GetReader("*3\n\r:1234\n\r+hi\n\r-me-error\n\r");

            var parsedObject = (RedisArray)reader.Single();

            Assert.Equal(3, parsedObject.Count);
            Assert.Equal(RedisType.Array, parsedObject.Type);

            var int1 = (RedisInteger)parsedObject.ElementAt(0);
            var str1 = (RedisString)parsedObject.ElementAt(1);
            var err1 = (RedisError)parsedObject.ElementAt(2);

            Assert.Equal(1234, int1.Value);
            Assert.Equal("hi", str1.AsString());
            Assert.Equal("me-error", err1.Message);
        }

        [Theory]
        [InlineData("hello")]
        [InlineData("abcdefghijklmnopqrstuvwxzy")]
        [InlineData("x-y-z")]
        [InlineData("1234")]
        [InlineData("<??>")]
        public void Read_BulkStringWithAsciiChars_ReturnsCorrectOutput(string str)
        {
            var reader = GetReader($"${str.Length}\n\r{str}\n\r");

            var parsedObject = (RedisString)reader.Single();
            var value = parsedObject.AsString();

            Assert.Equal(str, value);
            Assert.Equal(RedisType.String, parsedObject.Type);
        }

        [Fact]
        public void Read_Integer()
        {
            var reader = GetReader(":12345678\n\r");

            var parsedObject = (RedisInteger)reader.Single();
            var value = parsedObject.Value;

            Assert.Equal(12345678, value);
            Assert.Equal(RedisType.Integer, parsedObject.Type);
        }

        static DataReader GetReader(string data)
        {
            var stream = new MemoryStream(GetData(data));

            return new DataReader(new StreamIterator(stream));
        }

        static byte[] GetData(string value) => Encoding.ASCII.GetBytes(value);
    }
}
