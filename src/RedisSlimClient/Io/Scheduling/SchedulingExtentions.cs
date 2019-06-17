using System;
using System.Diagnostics;
using System.IO.Pipelines;

namespace RedisSlimClient.Io.Pipelines
{
    static class SchedulingExtentions
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