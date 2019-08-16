using System.Threading.Tasks;

namespace RedisSlimClient.Io.Scheduling
{
    interface ISchedulable
    {
        void Schedule(IWorkScheduler scheduler);

        Task RunAsync();

        Task Reset();
    }
}