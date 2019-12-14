using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.Types.Messaging
{
    public interface ISubscription
    {
        string[] Channels { get; }
        Task Unsubscribe(CancellationToken cancellation = default);
    }

    class Subscription : ISubscription
    {
        readonly Func<CancellationToken, Task> _unsubscribe;

        public Subscription(string[] channels, Func<CancellationToken, Task> unsubscribe)
        {
            _unsubscribe = unsubscribe;
        }

        public string[] Channels { get; }

        public Task Unsubscribe(CancellationToken cancellation) => _unsubscribe(cancellation);
    }
}
