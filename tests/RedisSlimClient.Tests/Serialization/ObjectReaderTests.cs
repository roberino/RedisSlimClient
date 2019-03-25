using RedisSlimClient.Serialization;
using RedisSlimClient.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace RedisSlimClient.Tests.Serialization
{
    public class ObjectReaderTests
    {
        [Fact]
        public void ReadString_SingleStringItem_ReturnsCorrectValue()
        {
            var parts = CreateParts("name1", TypeCode.String, SubType.None, GetString("value1"));

            var reader = new ObjectReader(parts);

            var str = reader.ReadString("name1");

            Assert.Equal("value1", str);
        }

        [Fact]
        public void ReadString_MultipleStringItems_ReturnsCorrectValue()
        {
            var parts = CreateParts("name1", TypeCode.String, SubType.None, GetString("value1"))
                .Concat(CreateParts("name2", TypeCode.String, SubType.None, GetString("value2")));

            var reader = new ObjectReader(parts);
            
            Assert.Equal("value1", reader.ReadString("name1"));
            Assert.Equal("value2", reader.ReadString("name2"));
        }

        [Fact]
        public void ReadString_MultipleStringItemsDifferentOrder_ReturnsCorrectlyOrderedValues()
        {
            var parts = CreateParts("name1", TypeCode.String, SubType.None, GetString("value1"))
                .Concat(CreateParts("name2", TypeCode.String, SubType.None, GetString("value2")));

            var reader = new ObjectReader(parts);

            Assert.Equal("value2", reader.ReadString("name2"));
            Assert.Equal("value1", reader.ReadString("name1"));
        }

        RedisString GetString(string value) => new RedisString(Encoding.ASCII.GetBytes(value));

        IEnumerable<RedisObjectPart> CreateParts(
            string name, TypeCode code, SubType subType,
            params RedisObject[] content)
        {
            var i = 3;

            return new[]
                {
                    new RedisObjectPart
                    {
                        ArrayIndex = 0,
                        Length = 4,
                        Value = new RedisString(Encoding.ASCII.GetBytes(name))
                    },
                    new RedisObjectPart
                    {
                        ArrayIndex = 1,
                        Length = 4,
                        Value = new RedisInteger((long) code)
                    },
                    new RedisObjectPart
                    {
                        ArrayIndex = 2,
                        Length = 4,
                        Value = new RedisInteger((long) subType)
                    }
                }
                .Concat(content.Select(c => new RedisObjectPart()
                {
                    ArrayIndex = i++,
                    Length = 4,
                    Value = c
                }));
        }
    }
}
