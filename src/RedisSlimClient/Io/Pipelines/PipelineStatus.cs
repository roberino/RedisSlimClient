namespace RedisSlimClient.Io.Pipelines
{
    enum PipelineStatus : byte
    {
        None,
        ReceivingFromSocket,
        ReadingFromPipe,
        SendingToSocket,
        WritingToPipe,
        Advancing,
        Faulted
    }
}
