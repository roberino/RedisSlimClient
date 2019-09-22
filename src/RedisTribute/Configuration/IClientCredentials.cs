namespace RedisTribute.Configuration
{
    interface IClientCredentials
    {
        int Id { get; }
        string ClientName { get; }
        IPasswordManager PasswordManager { get; }
    }
}