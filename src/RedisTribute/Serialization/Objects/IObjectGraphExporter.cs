namespace RedisTribute.Serialization
{
    public interface IObjectGraphExporter<T>
    {
        void WriteObjectData(T instance, IObjectWriter writer);
        T ReadObjectData(T instance, IObjectReader writer);
    }
}