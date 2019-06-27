using NSubstitute;
using RedisSlimClient.Configuration;
using RedisSlimClient.Io;
using RedisSlimClient.Io.Commands;
using RedisSlimClient.Serialization;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace RedisSlimClient.UnitTests
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
            var connection = Substitute.For<IConnection>();
            var pipeline = Substitute.For<ICommandPipeline>();
            
            connection.ConnectAsync().Returns(pipeline);
            pipeline.Execute(Arg.Any<ObjectSetCommand<MyData>>(), Arg.Any<CancellationToken>())
                .Returns(call =>
                {
                    var cmd = call.Arg<ObjectSetCommand<MyData>>();

                    cmd.GetArgs();

                    return true;
                });

            var arg = new MyData { X = 1 };

            var config = new ClientConfiguration("host1")
            {
                SerializerFactory = SetupSerializer(arg)
            };

            var client = new RedisClient(config, _ => connection);

            var result = await client.SetObjectAsync("x", arg);
            
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