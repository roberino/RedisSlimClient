using System;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.Io.Scheduling
{
    class ThreadPoolScheduler : IWorkScheduler
    {
        int _activeWork;

        ThreadPoolScheduler() { }

        public static ThreadPoolScheduler Instance { get; } = new ThreadPoolScheduler();

        public int ActiveWork => _activeWork;
        public event Action<int> Scheduling;

        public void Schedule(Func<Task> work)
        {
            var threads = Interlocked.Increment(ref _activeWork);

            Scheduling?.Invoke(threads);

            PipeScheduler.ThreadPool.Schedule(x =>
            {
                try
                {
                    work().ConfigureAwait(false).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex);
                }
                finally
                {
                    Interlocked.Decrement(ref _activeWork);
                }
            }, null);
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
            Scheduling = null;
        }
    }
}