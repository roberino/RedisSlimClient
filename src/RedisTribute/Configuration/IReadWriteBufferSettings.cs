namespace RedisTribute.Configuration
{
    public interface IReadWriteBufferSettings
    {
        int ReadBufferSize { get; set; }
        int WriteBufferSize { get; set; }
    }
}