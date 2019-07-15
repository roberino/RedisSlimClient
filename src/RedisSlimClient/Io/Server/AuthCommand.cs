using System;
using System.Security.Authentication;
using RedisSlimClient.Io.Commands;
using RedisSlimClient.Types;

namespace RedisSlimClient.Io.Server
{
    internal class AuthCommand : RedisCommand<bool>
    {
        private readonly string _password;

        public AuthCommand(string password) : base("AUTH")
        {
            _password = password;
        }

        public override object[] GetArgs() => new object[] { CommandText, _password };

        protected override Exception TranslateError(RedisError err)
        {
            return new AuthenticationException(err.Message);
        }

        protected override bool TranslateResult(IRedisObject redisObject)
        {
            return string.Equals(redisObject.ToString(), "Ok", System.StringComparison.OrdinalIgnoreCase);
        }
    }
}