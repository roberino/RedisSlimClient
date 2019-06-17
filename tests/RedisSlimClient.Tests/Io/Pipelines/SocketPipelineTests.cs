using RedisSlimClient.Io.Pipelines;
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

        [Fact]
        public async Task Sender_SendAsyncThenReceieve()
        {
            var received = new ConcurrentQueue<byte>();
            var socket = new StubSocket();
            var pipe = new SocketPipeline(socket);
            var waitHandle = new ManualResetEvent(false);
            var dataReceived = false;
            var hasError = false;

            pipe.Receiver.RegisterHandler(x =>
            {
                if (x.Length > 0)
                {
                    return x.GetPosition(x.Length);
                }

                return null;
            }, s =>
            {
                foreach(var m in s)
                {
                    foreach(var b in m.Span)
                    {
                        received.Enqueue(b);
                    }
                }
                dataReceived = true;
                waitHandle.Set();
            });

            pipe.Receiver.Error += e =>
            {
                TestOutput.WriteLine(e.ToString());
                hasError = true;
            };

            await pipe.Sender.SendAsync(x =>
            {
                x.Span[0] = 7;
                x.Span[1] = 3;
                x.Span[2] = 11;
                return 3;
            });

            TestExtensions.RunOnBackgroundThread(pipe.RunAsync);

            waitHandle.WaitOne(1000000);
            waitHandle.Dispose();

            var data = received.ToArray();

            Assert.True(dataReceived);
            Assert.False(hasError);

            Assert.Equal(3, data.Length);
            Assert.Equal(7, data[0]);
            Assert.Equal(3, data[1]);
            Assert.Equal(11, data[2]);
        }
    }
}