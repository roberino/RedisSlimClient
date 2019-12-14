namespace RedisTribute.Types.Messaging
{
    public interface IMessage
    {
        string Channel { get; }
        byte[] Body { get; }
    }
}
