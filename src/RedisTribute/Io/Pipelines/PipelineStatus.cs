namespace RedisTribute.Io.Pipelines
{
    enum PipelineStatus : byte
    {
        None = 0,
        Resetting,
        AwaitingConnection,
        AwaitingReset,
        ReceivingFromSocket,
        ReadingFromPipe,
        ReadFromPipe,
        ReadFromPipeEmpty,
        Delimiting,
        ProcessingData,
        ReadingMoreData,
        SendingToSocket,
        WritingToPipe,
        WritingToPipeComplete,
        AdvancingWriter,
        AdvancingReader,
        Flushing,
        Flushed,
        Faulted
    }
}