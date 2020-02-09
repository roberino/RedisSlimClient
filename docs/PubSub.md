# Publishing and Subscribing to Channels

The subscriber client offers a convenient interface for subscribing to messages published on
a channel. You can publish simple string and binary messages or more complex typed messages
which will be serialized across the channel.

## Basic usage

```cs
using (var client = config.CreateClient())
using (var subClient = config.CreateSubscriberClient())
{
    var channel = "my-channel";

    var subscription = await subClient.SubscribeAsync<MyMessage>(channel, m =>
    {
        return DoSomethingAsyncWithMessage(m);
    });

    var x = await client.PublishAsync(channel, new MyMessage()
    {
            Title = "Hey"
    }, new Dictionary<string, object>()
    {
        ["Sender"] = "from me"
    });

    await subscription.Unsubscribe(); // e.g. when app is shutting down
}

```