namespace RedisTribute.Io.Scheduling
{
    static class SchedulingExtensions
    {
        public static void ScheduleOnThreadpool(this ISchedulable schedulable)
        {
            schedulable.Schedule(ThreadPoolScheduler.Instance);
        }
    }
}