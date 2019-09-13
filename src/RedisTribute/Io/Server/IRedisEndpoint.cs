namespace RedisTribute.Io.Server
{
    public interface IRedisEndpoint
    {
        string Host { get; }
        int MappedPort { get; }
        int Port { get; }
        ServerRoleType RoleType { get; }
    }
}