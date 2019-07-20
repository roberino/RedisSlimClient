using RedisSlimClient.Util;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace RedisSlimClient.UnitTests.Util
{
    public class TimeThrottleTests
    {
        [Fact]
        public async Task TryRunAsync_FirstRun_IsExecuted()
        {
            var dates = new DateTime[] { DateTime.UtcNow };

            var throttle = new TimeThrottle(TimeSpan.FromSeconds(1), () => dates[0]);

            var wasCalled = false;

            await throttle.TryRunAsync(() =>
            {
                wasCalled = true;

                return Task.CompletedTask;
            });

            Assert.True(wasCalled);
        }

        [Fact]
        public async Task TryRunAsync_SecondRunWithinTimelimit_IsExecutedTwiceWithDelay()
        {
            var i = 0;
            var now = DateTime.UtcNow;
            var dates = Enumerable.Range(0, 10).Select(n => now.AddMilliseconds(n)).ToArray();

            var throttle = new TimeThrottle(TimeSpan.FromMilliseconds(100), () => dates[i++]);

            var callCounter = 0;

            var sw = new Stopwatch();


            await throttle.TryRunAsync(() =>
            {
                callCounter++;

                return Task.CompletedTask;
            });

            sw.Start();

            await throttle.TryRunAsync(() =>
            {
                callCounter++;

                return Task.CompletedTask;
            });

            sw.Stop();

            Assert.Equal(2, callCounter);
            Assert.True(sw.ElapsedMilliseconds > 90);
        }

        [Fact]
        public async Task TryRunAsync_ConsecutiveRuns_SomeAreNotExecuted()
        {
            var i = 0;
            var now = DateTime.UtcNow;
            var dates = Enumerable.Range(0, 400).Select(n => now.AddMilliseconds(n)).ToArray();

            var throttle = new TimeThrottle(TimeSpan.FromMilliseconds(100), () => dates[i++]);

            var callCounter = 0;

            var tasks = Enumerable.Range(1, 100).Select(n => throttle.TryRunAsync(() =>
            {
                callCounter++;

                return Task.CompletedTask;
            }));

            await Task.WhenAll(tasks);

            Assert.True(callCounter < 60);
        }
    }
}