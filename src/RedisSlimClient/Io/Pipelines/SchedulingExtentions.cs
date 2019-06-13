using System.IO.Pipelines;

namespace RedisSlimClient.Io.Pipelines
{
    static class SchedulingExtentions
    {
        public static void ScheduleOnThreadpool(this IRunnable runnable)
        {
            PipeScheduler.ThreadPool.Schedule(x => runnable.RunAsync().ConfigureAwait(false).GetAwaiter().GetResult(), null);
        }
    }
}