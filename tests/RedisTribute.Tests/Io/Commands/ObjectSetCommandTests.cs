using NSubstitute;
using RedisTribute.Configuration;
using RedisTribute.Io.Commands;
using RedisTribute.Serialization;
using RedisTribute.UnitTests.Serialization;
using System;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace RedisTribute.UnitTests.Io.Commands
{
    public class ObjectSetCommandTests
    {
        [Fact]
        public async Task GetArgs_ExceedDefaultBufferSize_ReturnsValidRedisArgs()
        {
            var settings = Substitute.For<ISerializerSettings>();
            var serializer = Substitute.For<IObjectSerializer<TestComplexDto>>();
            var instance = new TestComplexDto();

            settings.Encoding.Returns(Encoding.UTF8);
            settings.SerializerFactory.Returns(Substitute.For<IObjectSerializerFactory>());
            settings.SerializerFactory.Create<TestComplexDto>().Returns(serializer);
            serializer.When(s => s.WriteData(instance, Arg.Any<IObjectWriter>())).Do(call => call.Arg<IObjectWriter>().Raw(new byte[1000]));

            var command = new ObjectSetCommand<TestComplexDto>("x", settings, instance, default);

            object[] args = null;

            command.OnExecute = a =>
            {
                args = a;
                return Task.CompletedTask;
            };

            await command.Execute();

            var data = ((ArraySegment<byte>)args[2]);

            Assert.Equal("SET", args[0]);
            Assert.Equal((byte)'x', ((byte[])args[1])[0]);
            Assert.Equal(1000, data.Count);
        }
    }
}
