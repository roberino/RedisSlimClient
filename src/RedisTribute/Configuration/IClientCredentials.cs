namespace RedisTribute.Configuration
{
    interface IClientIdentifier
    {
        int Id { get; }
        string ClientName { get; }
    }

    interface IClientCredentials : IClientIdentifier
    {
        int Database { get; }
        IPasswordManager PasswordManager { get; }
    }
}