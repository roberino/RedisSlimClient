using System;
using System.Threading.Tasks;

namespace RedisSlimClient.Io.Scheduling
{
    public interface IWorkScheduler : IDisposable
    {
        void Schedule(Func<Task> work);
    }
}