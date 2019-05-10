namespace RedisSlimClient.Serialization
{
    public interface IObjectSerializer<T>
    {
        void WriteData(T instance, IObjectWriter writer);
        T ReadData(IObjectReader reader, T defaultValue);
    }
}