using RedisSlimClient.Io.Pipelines;
using System;
using System.Buffers;
using System.Collections.Concurrent;
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

                await pipe.Sender.SendAsync(x =>
                {
                    for (var v = 0; v < factor; v++)
                    {
                        for (var i = 0; i < frameSize; i++)
                        {
                            var n = (v * frameSize) + i;
                            x.Span[n] = (byte)(i == frameSize - 1 ? 'x' : n);
                            TestOutput.WriteLine(n.ToString());
                        }
                    }
                    return total;
                });

                pipe.ScheduleOnThreadpool();

                waitHandle.WaitOne(1000);
            }

            Assert.Equal(total, received.Count);
        }
    }
}