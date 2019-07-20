using System;
using System.Threading.Tasks;

namespace RedisSlimClient.Util
{
    interface IAsyncEvent<T>
    {
        void Subscribe(Func<T, Task> handler);
    }
}