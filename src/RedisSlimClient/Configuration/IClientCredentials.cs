namespace RedisSlimClient.Configuration
{
    interface IClientCredentials
    {
        int Id { get; }
        string ClientName { get; }
        string Password { get; }
    }
}