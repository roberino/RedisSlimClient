using RedisSlimClient.Serialization;
using RedisSlimClient.Types;
using System;
using System.Text;
using Xunit;

namespace RedisSlimClient.Tests.Serialization
{
    public class ObjectReaderTests
    {
        [Fact]
        public void ReadString_SingleItem_ReturnsCorrectValue()
        {
            var parts = new[]
            {
                new RedisObjectPart()
                {
                    ArrayIndex = 0,
                    Length = 4,
                    Value = new RedisString(Encoding.ASCII.GetBytes("name1"))
                },
                new RedisObjectPart()
                {
                    ArrayIndex = 1,
                    Length = 4,
                    Value = new RedisInteger((long)TypeCode.String)
                },
                new RedisObjectPart()
                {
                    ArrayIndex = 2,
                    Length = 4,
                    Value = new RedisInteger((long)SubType.None)
                },
                new RedisObjectPart()
                {
                    ArrayIndex = 3,
                    Length = 4,
                    Value = new RedisString(Encoding.ASCII.GetBytes("value1"))
                }
            };

            var reader = new ObjectReader(parts);

            var str = reader.ReadString("name1");

            Assert.Equal("value1", str);
        }
    }
}
