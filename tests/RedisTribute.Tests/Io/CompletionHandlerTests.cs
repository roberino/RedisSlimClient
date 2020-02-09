using NSubstitute;
using RedisTribute.Io;
using RedisTribute.Io.Commands;
using RedisTribute.Io.Pipelines;
using RedisTribute.Io.Scheduling;
using RedisTribute.Io.Server;
using RedisTribute.Types;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace RedisTribute.UnitTests.Io
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

            await queue.Enqueue(cmd);

            var handler = new CompletionHandler(queue, ThreadPoolScheduler.Instance).Attach(receiver);

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
