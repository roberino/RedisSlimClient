using System.Threading.Tasks;

namespace RedisSlimClient.Io.Scheduling
{
    interface IRunnable
    {
        Task RunAsync();

        Task Reset();
    }
}