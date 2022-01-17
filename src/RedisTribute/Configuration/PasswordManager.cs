using RedisTribute.Io.Server;
using System.Collections.Generic;

namespace RedisTribute.Configuration
{
    class PasswordManager : IPasswordManager
    {
        readonly IDictionary<string, string> _passwords;

        string? _globalPassword;

        public PasswordManager()
        {
            _passwords = new Dictionary<string, string>();
        }

        public void SetPassword(string host, int port, string password)
        {
            _passwords[$"{host}:{port}"] = password;
        }

        public void SetDefaultPassword(string password)
        {
            _globalPassword = password;
        }

        public string? GetPassword(IRedisEndpoint redisEndpoint)
        {
            if (_passwords.TryGetValue($"{redisEndpoint.Host}:{redisEndpoint.Port}", out var pwd))
            {
                return pwd;
            }

            return _globalPassword;
        }
    }
}