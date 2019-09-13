namespace RedisTribute.Io.Net
{
    enum ReceiveStatus : byte
    {
        None,
        CheckAvailable,
        Awaiting,
        Receiving,
        ReceivedSynchronously,
        ReceivedAsynchronously,
        Received,
        Completed,
        Faulted
    }
}
