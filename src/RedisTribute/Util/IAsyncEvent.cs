using System;
using System.Threading.Tasks;

namespace RedisTribute.Util
{
    interface IAsyncEvent<T>
    {
        void Subscribe(Func<T, Task> handler);
        void Subscribe(Action handler);
    }
}