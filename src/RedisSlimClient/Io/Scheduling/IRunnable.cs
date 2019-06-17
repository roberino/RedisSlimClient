using System.Threading.Tasks;

namespace RedisSlimClient.Io.Pipelines
{
    interface IRunnable
    {
        Task RunAsync();
    }
}