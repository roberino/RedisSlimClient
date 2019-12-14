using RedisTribute.Io.Commands;
using RedisTribute.Io.Scheduling;
using RedisTribute.Types;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace RedisTribute.Io
{
    class SubscriberCommandQueue : CommandQueue
    {
        readonly ConcurrentDictionary<ISubscriberCommand, bool> _persistentCommands;

        public override int QueueSize => _persistentCommands.Count + base.QueueSize;

        public SubscriberCommandQueue()
        {
            _persistentCommands = new ConcurrentDictionary<ISubscriberCommand, bool>();
        }

        public async override Task AbortAll(Exception ex, IWorkScheduler scheduler)
        {
            await base.AbortAll(ex, scheduler);

            var all = _persistentCommands.Keys.ToList();

            foreach (var cmd in all)
            {
                scheduler.Schedule(() =>
                {
                    cmd.Abandon(ex);
                    return Task.CompletedTask;
                });

                _persistentCommands.TryRemove(cmd, out _);
            }
        }

        protected override Func<Task> Bind(IRedisObject result)
        {
            foreach (var sub in _persistentCommands.Keys)
            {
                if (sub.CanReceive(result))
                {
                    return () => sub.ReceiveAsync(result);
                }
            }

            return base.Bind(result);
        }


        protected override void Attach(IRedisCommand command)
        {
            if (command is ISubscriberCommand sub)
            {
                _persistentCommands[sub] = true;
            }

            base.Attach(command);
        }

        public override void Dispose()
        {
            base.Dispose();

            foreach(var item in _persistentCommands.Keys)
            {
                item.Abandon(new ObjectDisposedException(nameof(SubscriberCommandQueue)));
            }

            _persistentCommands.Clear();
        }
    }
}
