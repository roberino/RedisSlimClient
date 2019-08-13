namespace RedisSlimClient.Io.Pipelines
{
    enum PipelineStatus : byte
    {
        None = 0,
        AwaitingConnection,
        ReceivingFromSocket,
        ReadingFromPipe,
        ReadFromPipe,
        ReadFromPipeEmpty,
        Delimiting,
        ProcessingData,
        ReadingMoreData,
        SendingToSocket,
        WritingToPipe,
        AdvancingWriter,
        AdvancingReader,
        Flushing,
        Flushed,
        Faulted
    }
}