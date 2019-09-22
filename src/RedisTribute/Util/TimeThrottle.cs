using System;
using System.Threading.Tasks;

namespace RedisTribute.Util
{
    class TimeThrottle
    {
        readonly long _maxInterval;
        readonly Func<DateTime> _currentTime;

        long _lastTicks = -1;
        volatile bool _working;

        public TimeThrottle(TimeSpan maxExecuteInterval, Func<DateTime> currentTime = null)
        {
            _maxInterval = maxExecuteInterval.Ticks;
            _currentTime = currentTime ?? (() => DateTime.UtcNow);
        }

        public async Task TryRunAsync(Func<Task> work)
        {
            if (_working)
            {
                return;
            }

            _working = true;

            try
            {
                var elapsed = CurrentTime - _lastTicks;

                if (_lastTicks > -1 && elapsed < _maxInterval)
                {
                    var waitTime = TimeSpan.FromTicks(_maxInterval - elapsed);
                    await Task.Delay(waitTime);
                }

                await work();

                _lastTicks = CurrentTime;
            }
            finally
            {
                _working = false;
            }
        }

        long CurrentTime => _currentTime().Ticks;
    }
}