using RedisSlimClient.Io.Pipelines;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace RedisSlimClient.UnitTests.Io.Scheduling
{
    public class SchedulingExtensionsTests
    {
        [Fact]
        public void ScheduleOnThreadpool_SomeRunnable_IsRunOnceOnly()
        {
            var runnable = new MyRunnable();

            runnable.ScheduleOnThreadpool();

            runnable.Wait();

            Thread.Sleep(5);

            Assert.Equal(1, runnable.Counter);
        }
    }

    class MyRunnable : IRunnable, IDisposable
    {
        readonly ManualResetEvent _handle = new ManualResetEvent(false);

        int _counter;


        public Task RunAsync()
        {
            return Task.WhenAll(RunImpl(), Task.Delay(5));
        }

        public void Wait() => _handle.WaitOne(10000);

        public void Dispose()
        {
            _handle.Dispose();
        }

        public int Counter => _counter;

        async Task RunImpl()
        {
            await Task.Delay(1);

            await Task.Run(() =>
            {
                Interlocked.Increment(ref _counter);

                _handle.Set();
            });
        }

        public void Reset()
        {
            _handle.Set();
        }
    }
}
