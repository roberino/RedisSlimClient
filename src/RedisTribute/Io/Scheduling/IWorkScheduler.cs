using System;
using System.Threading.Tasks;

namespace RedisTribute.Io.Scheduling
{
    public interface IWorkScheduler : IDisposable
    {
        int ActiveWork { get; }
        event Action<int> Scheduling; 
        void Schedule(Func<Task> work);
    }
}