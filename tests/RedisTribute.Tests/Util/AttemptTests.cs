using RedisTribute.Util;
using System;
using System.Threading.Tasks;
using Xunit;

namespace RedisTribute.UnitTests.Util
{
    public class AttemptTests
    {
        [Fact]
        public async Task WithExponentialBackoff_SomeTaskFunc_ExecutesTaskFunc()
        {
            var attempts = 0;

            await Attempt.WithExponentialBackoff(() =>
            {
                attempts++;
                return Task.CompletedTask;
            }, TimeSpan.FromSeconds(1));

            Assert.Equal(1, attempts);
        }

        [Fact]
        public async Task WithExponentialBackoff_BrokenTaskFunc_ExecutesMultipleTimesWithinExpectedTimeLimit()
        {
            const int expectedAttempts = 4;

            var attempts = 0;

            var elapsed = await Attempt.WithExponentialBackoff(() =>
            {
                attempts++;

                if (attempts < expectedAttempts)
                {
                    throw new TimeoutException();
                }

                return Task.CompletedTask;
            }, TimeSpan.FromMilliseconds(10));

            Assert.True(elapsed.TotalMilliseconds < 40);

            Assert.Equal(expectedAttempts, attempts);
        }
    }
}