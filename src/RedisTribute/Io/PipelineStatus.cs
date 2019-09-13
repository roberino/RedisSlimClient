namespace RedisTribute.Io
{
    enum PipelineStatus : byte
    {
        Disabled,
        Uninitialized,
        Broken,
        Reinitializing,
        Ok
    }
}