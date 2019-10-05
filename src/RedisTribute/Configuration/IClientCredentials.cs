namespace RedisTribute.Configuration
{
    interface IClientCredentials
    {
        int Id { get; }
        int Database { get; }
        string ClientName { get; }
        IPasswordManager PasswordManager { get; }
    }
}