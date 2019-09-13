using System.Threading.Tasks;

namespace RedisTribute.Util
{
    static class TaskExtensions
    {
        public static void SyncExec(this Task task)
        {
            task.ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }
}