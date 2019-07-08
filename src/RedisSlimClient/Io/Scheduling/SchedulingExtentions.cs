using System.Threading.Tasks;

namespace RedisSlimClient.Io.Scheduling
{
    static class SchedulingExtensions
    {
        public static Task ScheduleOnThreadpool(this IRunnable runnable)
        {
            return ThreadPoolScheduler.Instance.ScheduleWithHandle(runnable.RunAsync);
        }
    }
}