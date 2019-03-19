using RedisSlimClient.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace RedisSlimClient.Tests.Serialization
{
    public class TypeModelTests
    {
        private readonly ITestOutputHelper _testOutput;

        public TypeModelTests(ITestOutputHelper testOutput)
        {
            _testOutput = testOutput;
        }

        [Fact]
        public void GetData_SimpleType_ReturnsPropertyData()
        {
            var model = TypeModel<AnotherTestDto>.Instance;

            var items = model.GetData(new AnotherTestDto()
                {
                    DataItem1 = "abc"
                }
            );

            Assert.Equal(1, items.Count);
            Assert.Equal("abc", items[nameof(AnotherTestDto.DataItem1)]);
        }

        [Fact]
        public void WriteData_SimpleType_WritesPropertyData()
        {
            var model = TypeModel<AnotherTestDto>.Instance;
            var writer = new ObjectWriter(new MemoryStream());

            model.WriteData(new AnotherTestDto
                {
                    DataItem1 = "abc"
                },
                writer
            );
        }

        [Fact]
        public void WriteData_MultiPropertyType_ReturnsPropertyData()
        {
            var now = DateTime.UtcNow;

            var model = TypeModel<TestDto>.Instance;
            var output = new MemoryStream();
            var writer = new ObjectWriter(output);

            model.WriteData(new TestDto()
            {
                DataItem1 = "abc",
                DataItem2 = now,
                DataItem3 = new AnotherTestDto()
                {
                    DataItem1 = "efg"
                }
            }, writer);

            _testOutput.WriteLine(Encoding.UTF8.GetString(output.ToArray()));
        }

        [Fact]
        public void GetData_NullReference_ReturnsPropertyData()
        {
            var model = TypeModel<AnotherTestDto>.Instance;

            var items = model.GetData(new AnotherTestDto()
                {
                    DataItem1 = null
                }
            );

            Assert.Equal(1, items.Count);
            Assert.Null(items[nameof(AnotherTestDto.DataItem1)]);
        }

        [Fact]
        public void GetData_TwoTypes_ReturnsPropertyData()
        {
            var model1 = TypeModel<AnotherTestDto>.Instance;
            var model2 = TypeModel<OtherTestDto>.Instance;

            var items1 = model1.GetData(new AnotherTestDto()
                {
                    DataItem1 = "abc"
                }
            );

            var items2 = model2.GetData(new OtherTestDto()
                {
                    DataItem1 = 123
                }
            );

            Assert.Equal(1, items1.Count);
            Assert.Equal("abc", items1[nameof(AnotherTestDto.DataItem1)]);
            Assert.Equal(1, items2.Count);
            Assert.Equal(123, items2[nameof(AnotherTestDto.DataItem1)]);
        }

        [Fact]
        public void GetData_MultiPropertyType_ReturnsPropertyData()
        {
            var now = DateTime.UtcNow;

            var model = TypeModel<TestDto>.Instance;

            var items = model.GetData(new TestDto()
            {
                DataItem1 = "abc",
                DataItem2 = now,
                DataItem3 = new AnotherTestDto()
                {
                    DataItem1 = "efg"
                }
            });

            Assert.Equal(3, items.Count);
            Assert.Equal("abc", items[nameof(TestDto.DataItem1)]);
            Assert.Equal(now, items[nameof(TestDto.DataItem2)]);
            Assert.Equal("efg", ((IDictionary<string, object>)items[nameof(TestDto.DataItem3)])[nameof(AnotherTestDto.DataItem1)]);
        }
    }
}