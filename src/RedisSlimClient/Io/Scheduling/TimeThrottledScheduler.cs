using RedisSlimClient.Util;
using System;
using System.Threading.Tasks;

namespace RedisSlimClient.Io.Scheduling
{
    class TimeThrottledScheduler : IWorkScheduler
    {
        readonly TimeThrottle _timeThrottle;
        readonly IWorkScheduler _baseScheduler;

        public TimeThrottledScheduler (IWorkScheduler baseScheduler, TimeSpan throttleTime)
        {
            _timeThrottle = new TimeThrottle(throttleTime);
            _baseScheduler = baseScheduler;
        }

        public void Schedule(Func<Task> work)
        {
            _baseScheduler.Schedule(() => _timeThrottle.TryRunAsync(work));
        }

        public void Dispose()
        {
            _baseScheduler.Dispose();
        }
    }
}