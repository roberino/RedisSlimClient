using System;

namespace RedisSlimClient.Io
{
    interface IWorkScheduler : IDisposable
    {
        void Awake();
        void Schedule(Func<bool> work);
    }
}