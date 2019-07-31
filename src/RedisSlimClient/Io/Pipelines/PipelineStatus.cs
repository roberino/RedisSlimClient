namespace RedisSlimClient.Io.Pipelines
{
    enum PipelineStatus : byte
    {
        None = 0,
        ReceivingFromSocket,
        ReadingFromPipe,
        SendingToSocket,
        WritingToPipe,
        Advancing,
        Faulted
    }
}