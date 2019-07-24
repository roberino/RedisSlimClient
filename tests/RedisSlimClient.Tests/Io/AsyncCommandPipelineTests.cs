using NSubstitute;
using RedisSlimClient.Io;
using RedisSlimClient.Io.Commands;
using RedisSlimClient.Io.Pipelines;
using RedisSlimClient.Io.Scheduling;
using RedisSlimClient.Io.Server;
using RedisSlimClient.Telemetry;
using RedisSlimClient.Types;
using RedisSlimClient.UnitTests.Io.Pipelines;
using System;
using System.Threading.Tasks;
using Xunit;

namespace RedisSlimClient.UnitTests.Io
{
    public class AsyncCommandPipelineTests
    {
        [Fact]
        public async Task Execute_SomeCommand_ReturnsResult()
        {
            using (var socket = new StubSocket())
            using (var socketPipe = new SocketPipeline(socket))
            using (var pipeline = new AsyncCommandPipeline(socketPipe, socket, ThreadPoolScheduler.Instance, NullWriter.Instance))
            {
                var command = Substitute.For<IRedisResult<IRedisObject>>();
                var taskCompletion = new TaskCompletionSource<IRedisObject>();

                command.When(c => c.Execute()).Do(call => command.OnExecute(new object[] { "GET", "X" }));
                command.GetAwaiter().Returns(taskCompletion.Task.GetAwaiter());
                command.When(cmd => cmd.Abandon(Arg.Any<Exception>())).Do(call =>
                {
                    taskCompletion.SetException(call.Arg<Exception>());
                });
                command.When(cmd => cmd.Complete(Arg.Any<IRedisObject>())).Do(call =>
                {
                    var obj = call.Arg<IRedisObject>();

                    taskCompletion.SetResult(obj);
                });

                var init = await pipeline.ExecuteAdmin(new PingCommand());
                var result = await pipeline.Execute(command);
            }
        }

        [Fact]
        public async Task Execute_SocketFailure_Reconnects()
        {
            using (var socket = new StubSocket())
            using (var socketPipe = new SocketPipeline(socket))
            using (var pipeline = new AsyncCommandPipeline(socketPipe, socket, ThreadPoolScheduler.Instance, NullWriter.Instance))
            {
                Assert.Equal(0, socket.CallsToConnect);

                await pipeline.ExecuteAdmin(new PingCommand());

                socket.RaiseError();

                var timeoutCount = 0;

                while (socket.CallsToConnect == 0)
                {
                    await Task.Delay(5);

                    if (timeoutCount++ > 100)
                    {
                        break;
                    }
                }

                Assert.Equal(1, socket.CallsToConnect);
            }
        }
    }
}