using RedisSlimClient.Io;
using RedisSlimClient.Serialization;
using System;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace RedisSlimClient.UnitTests.Serialization
{
    public class SerializerFactoryTests
    {
        readonly ITestOutputHelper _testOutput;
        readonly MemoryStream _output;
        readonly ObjectWriter _writer;

        public SerializerFactoryTests(ITestOutputHelper testOutput)
        {
            _testOutput = testOutput;
            _output = new MemoryStream();
            _writer = new ObjectWriter(_output);
        }

        [Fact]
        public void WriteData_SimpleType_WritesPropertyData()
        {
            WhenWritingObject(new TestDtoWithString
                {
                    DataItem1 = "abc"
                }
            );

            ThenOutputIsValid<TestDtoWithString>(x => Assert.Equal("abc", x.DataItem1));
        }

        [Fact]
        public void WriteData_ObjectCollection_CanWriteAndRead()
        {
            WhenWritingObject(new TestDtoWithGenericCollection<TestDtoWithInt> { Items = new[] { new TestDtoWithInt() { DataItem1 = 123 }, new TestDtoWithInt() { DataItem1 = 456 } } } );

            ThenOutputIsValid<TestDtoWithGenericCollection<TestDtoWithInt>>(x =>
            {
                Assert.Equal(123, x.Items[0].DataItem1);
                Assert.Equal(456, x.Items[1].DataItem1);
            });
        }

        [Theory]
        [InlineData(10)]
        [InlineData(36)]
        [InlineData(50)]
        [InlineData(100)]
        [InlineData(500)]
        public void WriteData_DifferentObjectCollectionSizes_CanWriteAndRead(int numberOfItems)
        {
            WhenWritingObject(new TestDtoWithGenericCollection<TestDtoWithInt>
            {
                Items = Enumerable.Range(1, numberOfItems).Select(x => new TestDtoWithInt()
                {
                    DataItem1 = x
                }).ToList()
            });

            ThenOutputIsValid<TestDtoWithGenericCollection<TestDtoWithInt>>(x =>
            {
                var i = 1;
                Assert.Equal(numberOfItems, x.Items.Count);
                Assert.True(x.Items.All(v => v.DataItem1 == i++));
            });
        }

        [Fact]
        public void WriteData_PrimativeCollection_CanWriteAndRead()
        {
            WhenWritingObject(new TestDtoWithGenericCollection<int> { Items = new[] { 123, 456 } });

            ThenOutputIsValid<TestDtoWithGenericCollection<int>>(x =>
            {
                Assert.Equal(123, x.Items[0]);
                Assert.Equal(456, x.Items[1]);
            });
        }

        [Fact]
        public void WriteData_StringCollection_CanWriteAndRead()
        {
            WhenWritingObject(new TestDtoWithGenericCollection<string> { Items = new[] { "abc", "def" } });

            ThenOutputIsValid<TestDtoWithGenericCollection<string>>(x =>
            {
                Assert.Equal("abc", x.Items[0]);
                Assert.Equal("def", x.Items[1]);
            });
        }

        [Fact]
        public void WriteData_GenericCollection_CollectionIsWritable()
        {
            WhenWritingObject(new TestDtoWithGenericCollection<string> { Items = new[] { "abc", "def" } });

            ThenOutputIsValid<TestDtoWithGenericCollection<string>>(x =>
            {
                Assert.False(x.Items.IsReadOnly);

                x.Items.Add("hij");
            });
        }

        [Fact]
        public void WriteData_StringGenericProperty_CanWriteAndRead()
        {
            WhenWritingObject(new TestDtoWithGeneric<string> { DataItem1 = "abc" });

            ThenOutputIsValid<TestDtoWithGeneric<string>>(x =>
            {
                Assert.Equal("abc", x.DataItem1);
            });
        }

        [Fact]
        public void WriteData_DecimalGenericProperty_CanWriteAndRead()
        {
            WhenWritingObject(new TestDtoWithGeneric<decimal> { DataItem1 = 123.456m });

            ThenOutputIsValid<TestDtoWithGeneric<decimal>>(x =>
            {
                Assert.Equal(123.456m, x.DataItem1);
            });
        }

        [Fact]
        public void WriteData_ByteArrayGenericProperty_CanWriteAndRead()
        {
            WhenWritingObject(new TestDtoWithGeneric<byte[]> { DataItem1 = new byte[] { 1, 2, 3 } });

            ThenOutputIsValid<TestDtoWithGeneric<byte[]>>(x =>
            {
                Assert.Equal(3, x.DataItem1.Length);
                Assert.Equal(1, x.DataItem1[0]);
                Assert.Equal(2, x.DataItem1[1]);
                Assert.Equal(3, x.DataItem1[2]);
            });
        }

        [Fact]
        public void WriteData_BoolGenericProperty_CanWriteAndRead()
        {
            WhenWritingObject(new TestDtoWithGeneric<bool> { DataItem1 = true });

            ThenOutputIsValid<TestDtoWithGeneric<bool>>(x =>
            {
                Assert.True(x.DataItem1);
            });
        }

        [Fact]
        public void WriteData_MultiPropertyType_ReturnsPropertyData()
        {
            var now = DateTime.UtcNow;

            WhenWritingObject(new TestComplexDto()
            {
                DataItem1 = "abc",
                DataItem2 = now,
                DataItem3 = new TestDtoWithString()
                {
                    DataItem1 = "efg"
                }
            });

            ThenOutputIsValid<TestComplexDto>(x =>
            {
                Assert.Equal("abc", x.DataItem1);
                Assert.Equal(now, x.DataItem2);
                Assert.Equal("efg", x.DataItem3.DataItem1);
            });
        }

        [Fact]
        public void WriteData_NullReference_CanSerializeAndDeserialize()
        {
            WhenWritingObject(new TestComplexDto()
            {
                DataItem1 = "abc",
                DataItem2 = DateTime.Now,
                DataItem3 = null
            });

            ThenOutputIsValid<TestComplexDto>(x =>
            {
                Assert.Null(x.DataItem3);
            });
        }

        [Fact]
        public void WriteData_CollectionPropertyType_ReturnsPropertyData()
        {
            WhenWritingObject(new TestDtoWithCollection()
            {
                DataItems = new[]
                {
                    new TestDtoWithString()
                    {
                        DataItem1 = "i1"
                    },
                    new TestDtoWithString()
                    {
                        DataItem1 = "i2"
                    }
                }
            });

            ThenOutputIsValid<TestDtoWithCollection>(x =>
            {
                Assert.Equal(2, x.DataItems.Length);
                Assert.Equal("i1", x.DataItems[0].DataItem1);
                Assert.Equal("i2", x.DataItems[1].DataItem1);
            });
        }

        void WhenWritingObject<T>(T obj)
        {
            SerializerFactory.Instance.Create<T>().WriteData(obj, _writer);
        }

        T ReadObject<T>()
        {
            _output.Position = 0;
            var iterator = new StreamIterator(_output);
            var objectStream = new RedisByteSequenceReader(iterator);
            var reader = new ObjectReader(objectStream);
            return SerializerFactory.Instance.Create<T>().ReadData(reader, default);
        }

        void ThenOutputIsValid()
        {
            _testOutput.WriteLine(Encoding.UTF8.GetString(_output.ToArray()));
        }

        void ThenOutputIsValid<T>(Action<T> assertion = null)
        {
            ThenOutputIsValid();

            var obj = ReadObject<T>();

            assertion?.Invoke(obj);
        }
    }
}