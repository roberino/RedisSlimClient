using System;

namespace RedisSlimClient.Io.Scheduling
{
    interface IWorkScheduler : IDisposable
    {
        void Awake();
        void Schedule(Func<bool> work);
    }
}