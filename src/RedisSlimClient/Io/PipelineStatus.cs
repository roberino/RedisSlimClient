namespace RedisSlimClient.Io
{
    enum PipelineStatus : byte
    {
        Uninitialized,
        Broken,
        Reinitializing,
        Ok
    }
}