using RedisTribute.Serialization;
using RedisTribute.Types.Messaging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute
{
    public static class PublishExtensions
    {
        public static Task<ISubscription> SubscribeAsync<T>(this ISubscriptionClient client, string channel, Func<IMessage<T>, Task> handler, CancellationToken cancellation = default)
            => client.SubscribeAsync(new[] { channel }, handler, cancellation);

        public static Task<ISubscription> SubscribeAsync(this ISubscriptionClient client, string channel, Func<IMessageData, Task> handler, CancellationToken cancellation = default)
            => client.SubscribeAsync(new[] { channel }, handler, cancellation);

        public static Task<int> PublishAsync(this IPublisherClient client, string channel, byte[] message, CancellationToken cancellation = default)
            => client.PublishAsync(new Message(message, channel), cancellation);

        public static Task<int> PublishAsync(this IPublisherClient client, string channel, string message, CancellationToken cancellation = default)
            => client.PublishAsync(new Message(message, channel), cancellation);

        public static Task<int> PublishAsync<T>(this IPublisherClient client, string channel, T message, CancellationToken cancellation = default)
            => client.PublishAsync(s => new Message<T>(message, channel, s), cancellation);

        public static Task<int> PublishAsync<T>(this IPublisherClient client, string channel, T message, IEnumerable<KeyValuePair<string, object>> properties, CancellationToken cancellation = default)
            => client.PublishAsync(s =>
            {
                var msg = new Message<T>(message, channel, s);

                foreach(var prop in properties)
                {
                    msg.Properties[prop.Key] = prop.Value.ToPrimativeString();
                }

                return msg;
            }, cancellation);

        public static Task<int> PublishAsync<T>(this IPublisherClient client, string channel, T message, IEnumerable<KeyValuePair<string, object>> properties, MessageFlags flags, CancellationToken cancellation = default)
            => client.PublishAsync(s =>
            {
                var msg = new Message<T>(message, channel, s, flags);

                foreach (var prop in properties)
                {
                    msg.Properties[prop.Key] = prop.Value.ToPrimativeString();
                }

                return msg;
            }, cancellation);
    }
}
