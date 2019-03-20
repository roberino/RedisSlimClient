﻿using RedisSlimClient.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace RedisSlimClient.Tests.Serialization
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
            WhenWritingObject(new AnotherTestDto
                {
                    DataItem1 = "abc"
                }
            );

            ThenOutputIsValid();
        }

        [Fact]
        public void WriteData_MultiPropertyType_ReturnsPropertyData()
        {
            var now = DateTime.UtcNow;

            WhenWritingObject(new TestDto()
            {
                DataItem1 = "abc",
                DataItem2 = now,
                DataItem3 = new AnotherTestDto()
                {
                    DataItem1 = "efg"
                }
            });

            ThenOutputIsValid();
        }

        [Fact]
        public void WriteData_CollectionPropertyType_ReturnsPropertyData()
        {
            WhenWritingObject(new TestDtoWithCollection()
            {
                DataItems = new[]
                {
                    new AnotherTestDto()
                    {
                        DataItem1 = "1"
                    }
                }
            });

            ThenOutputIsValid();
        }

        void WhenWritingObject<T>(T obj)
        {
            SerializerFactory.Instance.Create<T>().WriteData(obj, _writer);
        }

        void ThenOutputIsValid()
        {
            _testOutput.WriteLine(Encoding.UTF8.GetString(_output.ToArray()));
        }
    }
}