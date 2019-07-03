using RedisSlimClient.Serialization;
using RedisSlimClient.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using RedisSlimClient.Io;
using Xunit;
using Xunit.Abstractions;

namespace RedisSlimClient.UnitTests.Serialization
{
    public class ObjectReaderTests
    {
        readonly ITestOutputHelper testOutput;

        public ObjectReaderTests(ITestOutputHelper testOutput)
        {
            this.testOutput = testOutput;
        }

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

        [Fact]
        public void ReadObject_SubObject_ReturnsCorrectData()
        {
            var reader = CreateReader(w =>
            {
                w.BeginWrite(2);
                w.WriteItem("name1", "value1");
                w.WriteItem("name2", new TestDtoWithString()
                {
                    DataItem1 = "value2"
                });
            });

            reader.BeginRead(2);

            Assert.Equal("value1", reader.ReadString("name1"));

            var subOb = reader.ReadObject<TestDtoWithString>("name2", null);

            Assert.Equal("value2", subOb.DataItem1);
        }

        [Fact]
        public void ReadObject_MisOrderedRead_ReturnsCorrectData()
        {
            var reader = CreateReader(w =>
            {
                w.BeginWrite(3);
                w.WriteItem("name1", "value1");
                w.WriteItem("name3", "value3");
                w.WriteItem("name2", new TestDtoWithString()
                {
                    DataItem1 = "value2"
                });
            });

            // reader.Dump(testOutput.WriteLine);

            reader.BeginRead(3);

            Assert.Equal("value1", reader.ReadString("name1"));

            var subOb = reader.ReadObject<TestDtoWithString>("name2", null);

            Assert.Equal("value2", subOb.DataItem1);

            Assert.Equal("value3", reader.ReadString("name3"));
        }

        RedisString GetString(string value) => new RedisString(Encoding.ASCII.GetBytes(value));

        ObjectReader CreateReader(Action<ObjectWriter> write)
        {
            using (var data = new MemoryStream())
            {
                var writer = new ObjectWriter(data);

                write.Invoke(writer);

                data.Position = 0;

                var iterator = new StreamIterator(data);
                var byteReader = new ArraySegmentToRedisObjectReader(iterator).ToArray();

                return new ObjectReader(byteReader);
            }
        }

        IEnumerable<RedisObjectPart> CreateParts(
            string name, TypeCode code, SubType subType,
            params RedisObject[] content)
        {
            var i = 3;
            var level = 1;
            var len = content.Length + i;

            return new[]
                {
                    new RedisObjectPart()
                    {
                        IsArrayStart = true,
                        Length = len,
                        Level = level
                    },
                    new RedisObjectPart
                    {
                        ArrayIndex = 0,
                        Length = len,
                        Value = new RedisString(Encoding.ASCII.GetBytes(name))
                    },
                    new RedisObjectPart
                    {
                        ArrayIndex = 1,
                        Length = len,
                        Value = new RedisInteger((long) code)
                    },
                    new RedisObjectPart
                    {
                        ArrayIndex = 2,
                        Length = len,
                        Value = new RedisInteger((long) subType)
                    }
                }
                .Concat(content.Select(c => new RedisObjectPart()
                {
                    ArrayIndex = i++,
                    Length = len,
                    Value = c
                }));
        }
    }
}