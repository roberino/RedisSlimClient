using System;
using System.Threading.Tasks;

namespace RedisTribute.Io.Scheduling
{
    interface ISchedulable
    {
        void Schedule(IWorkScheduler scheduler);

        Task RunAsync();
    }
}