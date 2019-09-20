using NSubstitute;
using RedisTribute.Configuration;
using RedisTribute.Io;
using RedisTribute.Io.Commands;
using RedisTribute.Serialization;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace RedisTribute.UnitTests
{
    public class RedisClientTests
    {
        IList<object> _store;

        public RedisClientTests()
        {
            _store = new List<object>();
        }

        [Fact]
        public async Task SetObjectAsync_WritesObjectDataToStore()
        {
            var connection = Substitute.For<ICommandRouter>();
            var pipeline = Substitute.For<ICommandPipeline>();
            
            connection.RouteCommandAsync(Arg.Any<ICommandIdentity>()).Returns(pipeline);
            pipeline.Execute(Arg.Any<ObjectSetCommand<MyData>>(), Arg.Any<CancellationToken>())
                .Returns(async call =>
                {
                    var cmd = call.Arg<ObjectSetCommand<MyData>>();

                    cmd.OnExecute = x => Task.CompletedTask;

                    await cmd.Execute();

                    return true;
                });

            var arg = new MyData { X = 1 };

            var config = new ClientConfiguration("host1")
            {
                SerializerFactory = SetupSerializer(arg)
            };

            var client = new RedisClient(new RedisController(config, _ => connection));

            var result = await client.SetAsync("x", arg);
            
            Assert.Same(_store[0], arg);
        }

        IObjectSerializerFactory SetupSerializer<T>(T defaultValue)
        {
            var factory = Substitute.For<IObjectSerializerFactory>();
            var serializer = Substitute.For<IObjectSerializer<T>>();
            
            factory.Create<T>().Returns(serializer);

            serializer.When(s => s.WriteData(Arg.Any<T>(), Arg.Any<IObjectWriter>()))
                .Do(call => _store.Add(call.Arg<T>()));            

            return factory;
        }

        public class MyData
        {
            public int X { get; set; }
        }
    }
}