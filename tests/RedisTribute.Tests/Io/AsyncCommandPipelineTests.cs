using RedisTribute.Io;
using RedisTribute.Io.Commands;
using RedisTribute.Io.Pipelines;
using RedisTribute.Io.Scheduling;
using RedisTribute.Io.Server;
using RedisTribute.Telemetry;
using RedisTribute.UnitTests.Io.Pipelines;
using System.Threading.Tasks;
using Xunit;

namespace RedisTribute.UnitTests.Io
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
                var command = new GetCommand("X");
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

                socket.WaitForConnect();

                Assert.Equal(1, socket.CallsToConnect);
            }
        }

        [Fact]
        public async Task Execute_SocketFailureAndConnectFailure_CallsReconnect()
        {
            using (var socket = new StubSocket())
            using (var socketPipe = new SocketPipeline(socket))
            using (var pipeline = new AsyncCommandPipeline(socketPipe, socket, ThreadPoolScheduler.Instance, NullWriter.Instance))
            {
                Assert.Equal(0, socket.CallsToConnect);

                await pipeline.ExecuteAdmin(new PingCommand());

                socket.BreakReconnection();
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

                Assert.True(socket.CallsToConnect > 0);
            }
        }
    }
}