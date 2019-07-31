namespace RedisSlimClient.Io.Net
{
    enum ReceiveStatus : byte
    {
        None,
        Awaiting,
        ReceivedSynchronously,
        ReceivedAsynchronously,
        Completed,
        Faulted
    }
}
