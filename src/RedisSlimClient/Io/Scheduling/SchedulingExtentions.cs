using System;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Threading.Tasks;

namespace RedisSlimClient.Io.Pipelines
{
    static class SchedulingExtensions
    {
        public static Task ScheduleOnThreadpool(this IRunnable runnable)
        {
            var result = new TaskCompletionSource<bool>();

            PipeScheduler.ThreadPool.Schedule(x =>
            {
                try
                {
                    runnable.RunAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                    result.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    result.TrySetException(ex);
                    Trace.WriteLine(ex);
                }
            }, null);

            return result.Task;
        }
    }
}