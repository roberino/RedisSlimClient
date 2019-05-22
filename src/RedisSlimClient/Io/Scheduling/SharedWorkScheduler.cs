using System;
using System.Threading;

namespace RedisSlimClient.Io.Scheduling
{
    class SharedWorkScheduler : IWorkScheduler
    {
        readonly Thread[] _workers;
        readonly string[] _work;

        public void Schedule(Func<bool> work)
        {
            throw new NotImplementedException();
        }

        public void Awake()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        class WorkItem
        {
            public Func<bool> Work;
            public bool Running;
        }
    }
}
