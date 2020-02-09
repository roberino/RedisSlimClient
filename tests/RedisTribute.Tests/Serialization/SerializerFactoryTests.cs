using RedisTribute.Io;
using RedisTribute.Serialization;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Xunit;
using Xunit.Abstractions;

namespace RedisTribute.UnitTests.Serialization
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
        public void WriteData_AnonType_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => WhenWritingObject(new { x = 123, y = "hey" }));

            // TODO: 
            //ThenOutputIsValid(x =>
            //{
            //    Assert.Equal(123, x.x);
            //    Assert.Equal("hey", x.y);
            //}, new { x = 0, y = "" });
        }

        [Fact]
        public void WriteData_TupleWithXElement_WritesCorrectly()
        {
            var data = XDocument.Parse("<data id='3'>xxy</data>");
            var value = (key: 123, data: data.Root);

            WhenWritingObject(value);

            ThenOutputIsValid<(int key, XElement data)>(x =>
            {
                Assert.Equal(123, x.key);
                Assert.Equal("3", x.data.Attribute("id").Value);
            });
        }

        [Fact]
        public void WriteData_TupleType_WritesTupleElements()
        {
            WhenWritingObject((x: 123, y: "hey"));

            ThenOutputIsValid<(int x, string y)>(x =>
            {
                Assert.Equal(123, x.x);
                Assert.Equal("hey", x.y);
            });
        }

        [Fact]
        public void ReadData_TupleTypeWithExcessMembers_WritesTupleElements()
        {
            WhenWritingObject((x: 123, y: 456));

            ThenOutputIsValid<(int x, int y, int z)>(x =>
            {
                Assert.Equal(123, x.x);
                Assert.Equal(456, x.y);
                Assert.Equal(0, x.z);
            });
        }

        [Fact]
        public void ReadData_TupleTypeWithLessMembers_WritesTupleElements()
        {
            WhenWritingObject((x: 123, y: 456, z : 789));

            ThenOutputIsValid<(int x, int y)>(x =>
            {
                Assert.Equal(123, x.x);
                Assert.Equal(456, x.y);
            });
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
        public void WriteData_SimpleTypeWithDouble_CanSerializeAndDeserialize()
        {
            WhenWritingObject(new TestDtoWithDouble());

            ThenOutputIsValid<TestDtoWithDouble>(x => Assert.Equal(0d, x.DataItem1));
        }

        [Fact]
        public void WriteData_Enum_CanSerializeAndDeserialize()
        {
            WhenWritingObject(TestEnum.Value2);

            ThenOutputIsValid<TestEnum>(x => Assert.Equal(TestEnum.Value2, x));
        }

        [Fact]
        public void WriteData_FlagEnum_CanSerializeAndDeserialize()
        {
            WhenWritingObject(TestFlagEnum.Value2 | TestFlagEnum.Value3);

            ThenOutputIsValid<TestFlagEnum>(x => Assert.Equal(TestFlagEnum.Value2 | TestFlagEnum.Value3, x));
        }

        [Fact]
        public void WriteData_SimpleTypeWithTimeSpan_CanSerializeAndDeserialize()
        {
            WhenWritingObject(new TestDtoWithTimeSpan()
            {
                Time1 = TimeSpan.FromMilliseconds(156)
            });

            ThenOutputIsValid<TestDtoWithTimeSpan>(x => Assert.Equal(TimeSpan.FromMilliseconds(156), x.Time1));
        }

        [Fact]
        public void WriteData_SimpleTypeWithEnum_CanSerializeAndDeserialize()
        {
            WhenWritingObject(new TestDtoWithEnum()
            {
                 DataItem1 = TestEnum.Value2
            });

            ThenOutputIsValid<TestDtoWithEnum>(x => Assert.Equal(TestEnum.Value2, x.DataItem1));
        }

        [Fact]
        public void WriteData_SimpleTypeWithNullString_CanSerializeAndDeserialize()
        {
            WhenWritingObject(new TestDtoWithString
            {
                DataItem1 = null
            }
            );

            ThenOutputIsValid<TestDtoWithString>(x => Assert.Empty(x.DataItem1));
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
        public void WriteData_NullableBoolGenericProperty_CanWriteAndRead()
        {
            WhenWritingObject(new TestDtoWithGeneric<bool?> { DataItem1 = true });

            ThenOutputIsValid<TestDtoWithGeneric<bool?>>(x =>
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
            var objectStream = new ArraySegmentToRedisObjectReader(iterator);
            var reader = new ObjectReader(objectStream, _output);
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

        void ThenOutputIsValid<T>(Action<T> assertion, T example)
        {
            ThenOutputIsValid();

            var obj = ReadObject<T>();

            assertion?.Invoke(obj);
        }
    }
}