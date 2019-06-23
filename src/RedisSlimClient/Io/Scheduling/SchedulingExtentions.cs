using System;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Threading;

namespace RedisSlimClient.Io.Pipelines
{
    static class SchedulingExtensions
    {
        public static void ScheduleOnThreadpool(this IRunnable runnable)
        {
            PipeScheduler.ThreadPool.Schedule(x =>
            {
                try
                {
                    runnable.RunAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex);
                }
            }, null);
        }
    }
}