using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RedisSlimClient.Util
{
    class AsyncEvent<T> : IAsyncEvent<T>
    {
        readonly IList<Func<T, Task>> _handlers;

        public AsyncEvent()
        {
            _handlers = new List<Func<T, Task>>();
        }

        public void Subscribe(Func<T, Task> handler)
        {
            _handlers.Add(handler);
        }

        public async Task PublishAsync(T args)
        {
            if(_handlers.Count == 0)
            {
                return;
            }

            await Task.WhenAll(_handlers.Select(h => h(args)));
        }
    }
}