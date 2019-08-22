using System.Threading.Tasks;

namespace RedisSlimClient.Util
{
    static class TaskExtensions
    {
        public static void SyncExec(this Task task)
        {
            task.ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }
}