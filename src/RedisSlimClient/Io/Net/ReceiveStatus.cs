namespace RedisSlimClient.Io.Net
{
    enum ReceiveStatus : byte
    {
        None,
        CheckAvailable,
        Awaiting,
        ReceivedSynchronously,
        ReceivedAsynchronously,
        Completed,
        Faulted
    }
}
