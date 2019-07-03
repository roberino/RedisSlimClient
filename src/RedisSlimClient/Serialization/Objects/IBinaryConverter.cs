namespace RedisSlimClient.Serialization
{
    interface IBinaryConverter<T>
    {
        byte[] GetBytes(T value);

        T GetValue(byte[] data);
    }
}
