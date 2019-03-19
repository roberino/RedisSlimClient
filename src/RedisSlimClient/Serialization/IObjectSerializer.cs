namespace RedisSlimClient.Serialization
{
    public interface IObjectSerializer<T>
    {
        void WriteData(T instance, IObjectWriter writer);
    }
}