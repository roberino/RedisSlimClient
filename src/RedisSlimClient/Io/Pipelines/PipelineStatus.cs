namespace RedisSlimClient.Io.Pipelines
{
    enum PipelineStatus
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
