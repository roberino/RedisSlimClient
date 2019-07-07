using NSubstitute;
using RedisSlimClient.Io;
using RedisSlimClient.Io.Commands;
using RedisSlimClient.Io.Pipelines;
using RedisSlimClient.Io.Scheduling;
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
        public async Task Execute_StubSocket_ReturnsResult()
        {
            var socket = new StubSocket();

            using (var socketPipe = new SocketPipeline(socket))
            using (var pipeline = new AsyncCommandPipeline(socketPipe, ThreadPoolScheduler.Instance,  NullWriter.Instance))
            {
                var command = Substitute.For<IRedisResult<RedisObject>>();
                var taskCompletion = new TaskCompletionSource<RedisObject>();

                command.GetArgs().Returns(new object[] { "GET", "X" });
                command.GetAwaiter().Returns(taskCompletion.Task.GetAwaiter());
                command.When(cmd => cmd.Abandon(Arg.Any<Exception>())).Do(call =>
                {
                    taskCompletion.SetException(call.Arg<Exception>());
                });
                command.When(cmd => cmd.Complete(Arg.Any<RedisObject>())).Do(call =>
                {
                    var obj = call.Arg<RedisObject>();

                    taskCompletion.SetResult(obj);
                });

                var result = await pipeline.Execute(command);
            }
        }
    }
}