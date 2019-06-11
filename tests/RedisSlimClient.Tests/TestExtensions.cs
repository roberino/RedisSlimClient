using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisSlimClient.UnitTests
{
    static class TestExtensions
    {
        public static void RunOnBackgroundThread(this Func<Task> work)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                work().Wait();
            });
        }
    }
}