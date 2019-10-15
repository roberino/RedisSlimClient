using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.Io.Scheduling
{
    class HighPriorityScheduler : IWorkScheduler
    {
        readonly ConcurrentDictionary<int, Thread> _activeThreads;

        HighPriorityScheduler()
        {
            _activeThreads = new ConcurrentDictionary<int, Thread>();
        }

        public static IWorkScheduler Instance = new HighPriorityScheduler();

        public int ActiveWork => _activeThreads.Count;

        public event Action<int> Scheduling;

        public void Dispose()
        {
        }

        public void Schedule(Func<Task> work)
        {
            Scheduling?.Invoke(ActiveWork);

            var thread = new Thread(() =>
            {
                var task = work();
                task.ConfigureAwait(false).GetAwaiter().GetResult();
                var id = Thread.CurrentThread.ManagedThreadId;
                _activeThreads.TryRemove(id, out var t);
            })
            {
                Priority = ThreadPriority.Highest
            };

            _activeThreads[thread.ManagedThreadId] = thread;

            thread.Start();
        }
    }
}
