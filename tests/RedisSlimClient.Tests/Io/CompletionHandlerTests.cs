using NSubstitute;
using RedisSlimClient.Io;
using RedisSlimClient.Io.Commands;
using RedisSlimClient.Io.Pipelines;
using RedisSlimClient.Io.Server;
using RedisSlimClient.Types;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace RedisSlimClient.UnitTests.Io
{
    public class CompletionHandlerTests
    {
        [Fact]
        public async Task Handle_CompletesItem()
        {
            Action<ReadOnlySequence<byte>> handlerAction = null;

            var cmd = new PingCommand();

            var receiver = Substitute.For<IPipelineReceiver>();
            
            var data = new [] { GetData("$4\r\n"), GetData("PONG\r\n") };

            receiver
                .When(h => h.RegisterHandler(Arg.Any<Func<ReadOnlySequence<byte>, SequencePosition?>>()
                , Arg.Any<Action<ReadOnlySequence<byte>>>()))
                .Do(call =>
                {
                    handlerAction = call.Arg<Action<ReadOnlySequence<byte>>>();
                });
                

            var queue = new CommandQueue();

            await queue.Enqueue(() => Task.FromResult((IRedisCommand)cmd));

            var handler = new CompletionHandler(receiver, queue);

            handlerAction?.Invoke(data[0]);
            handlerAction?.Invoke(data[1]);

            var result = await cmd;

            Assert.True(result);
        }

        ReadOnlySequence<byte> GetData(string data)
        {
            return new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes(data));
        }
    }
}
