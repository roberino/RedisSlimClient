using System;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Threading.Tasks;

namespace RedisSlimClient.Io.Scheduling
{
    class ThreadPoolScheduler : IWorkScheduler
    {
        private ThreadPoolScheduler() { }

        public static ThreadPoolScheduler Instance { get; } = new ThreadPoolScheduler();

        public void Schedule(Func<Task> work)
        {
            ScheduleWithHandle(work);
        }

        public Task ScheduleWithHandle(Func<Task> work)
        {
            var result = new TaskCompletionSource<bool>();

            PipeScheduler.ThreadPool.Schedule(x =>
            {
                try
                {
                    work().ConfigureAwait(false).GetAwaiter().GetResult();
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

        public void Dispose()
        {
        }
    }
}