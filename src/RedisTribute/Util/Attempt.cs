using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.Util
{
    static class Attempt
    {
        public static async Task<TimeSpan> WithExponentialBackoff(Func<Task> task, TimeSpan maxBackoff, int? maxRetries = null, CancellationToken cancellation = default)
        {
            Debug.Assert(maxBackoff.TotalMilliseconds >= 10);

            var retryCount = 0;
            var elapsed = 0d;
            var backoff = Math.Log(maxBackoff.TotalMilliseconds);
            
            while (!cancellation.IsCancellationRequested)
            {
                try
                {
                    await task();
                    break;
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch
                {
                    if (maxRetries.HasValue && retryCount == maxRetries.Value)
                    {
                        throw;
                    }

                    backoff = Math.Min(maxBackoff.TotalMilliseconds, backoff * backoff);
                    
                    await Task.Delay(TimeSpan.FromMilliseconds(backoff), cancellation);

                    elapsed += backoff;
                    retryCount++;
                }
            }

            return TimeSpan.FromMilliseconds(elapsed);
        }
    }
}