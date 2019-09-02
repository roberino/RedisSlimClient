using RedisSlimClient.Io.Commands;
using RedisSlimClient.Io.Pipelines;
using RedisSlimClient.Io.Scheduling;
using RedisSlimClient.Io.Server;
using RedisSlimClient.Serialization.Protocol;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace RedisSlimClient.UnitTests.Io.Pipelines
{
    public class SocketPipelineTests
    {
        public SocketPipelineTests(ITestOutputHelper testOutput)
        {
            TestOutput = testOutput;
        }

        public ITestOutputHelper TestOutput { get; }

        [Fact]
        public async Task SendAsync_CommandExample()
        {
            var received = false;

            using (var socket = new StubSocket())
            using (var waitHandle = new ManualResetEvent(false))
            using (var pipe = new SocketPipeline(socket))
            {
                pipe.Receiver.RegisterHandler(x => x.PositionOf((byte)'\n'), s =>
                {
                    received = true;
                    waitHandle.Set();
                });

                pipe.Receiver.Error += e =>
                {
                    TestOutput.WriteLine(e.ToString());
                };

                await pipe.Sender.SendAsync(m =>
                {
                    var command = new PingCommand();
                    var formatter = new RedisByteFormatter(m);

                    return formatter.Write(command.GetArgs());
                });

                pipe.ScheduleOnThreadpool();

                waitHandle.WaitOne(1000);
            }

            Assert.True(received);
        }

        [Fact(Skip = "WIP")]
        public async Task Reset_SomeDataWriten_ResetsPipeline()
        {
            var chunks = new List<byte[]>();

            using (var socket = new StubSocket())
            using (var pipe = new SocketPipeline(socket))
            {
                pipe.Receiver.RegisterHandler(x => x.PositionOf((byte)0), s =>
                {
                    chunks.Add(s.ToArray());
                });

                pipe.Receiver.Error += e =>
                {
                    TestOutput.WriteLine(e.ToString());
                };

                pipe.ScheduleOnThreadpool();

                await pipe.Sender.SendAsync(async m =>
                {
                    await m.Write(new byte[] { 1, 2, 3, 0, 4, 5, 6 });
                });

                await pipe.ResetAsync();

                await pipe.Sender.SendAsync(async m =>
                {
                    await m.Write(new byte[] { 7, 8, 9, 0 });
                });
            }
        }

        [Theory]
        [InlineData(10, 5)]
        [InlineData(7, 7)]
        [InlineData(119, 11)]
        public async Task Sender_SendAsyncThenReceieve(int frameSize, int factor)
        {
            var total = frameSize * factor;
            
            var received = new ConcurrentQueue<byte>();

            using (var socket = new StubSocket())
            using (var waitHandle = new ManualResetEvent(false))
            using (var pipe = new SocketPipeline(socket))
            {
                pipe.Receiver.RegisterHandler(x => x.PositionOf((byte)'x'), s =>
                {
                    var last = '-';

                    foreach (var m in s)
                    {
                        foreach (var b in m.ToArray())
                        {
                            received.Enqueue(b);

                            last = (char)b;
                        }
                    }

                    Assert.Equal('x', last);

                    if (received.Count == total)
                    {
                        waitHandle.Set();
                    }
                });

                pipe.Receiver.Error += e =>
                {
                    TestOutput.WriteLine(e.ToString());
                };

                await pipe.Sender.SendAsync(async x =>
                {
                    for (var v = 0; v < factor; v++)
                    {
                        for (var i = 0; i < frameSize; i++)
                        {
                            var n = (v * frameSize) + i;
                            await x.Write((byte)(i == frameSize - 1 ? 'x' : n));
                        }
                    }
                });

                pipe.ScheduleOnThreadpool();

                waitHandle.WaitOne(3000);
            }

            Assert.Equal(total, received.Count);
        }
    }
}