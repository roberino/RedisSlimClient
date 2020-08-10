using RedisTribute.Configuration;
using RedisTribute.Serialization;
using RedisTribute.Types;
using System;

namespace RedisTribute.Io.Commands
{
    class ObjectSetCommand<T> : RedisCommand<bool>
    {
        static readonly object _lockObj = new object();

        readonly ISerializerSettings _configuration;
        readonly T _objectData;
        readonly SetOptions _options;

        public ObjectSetCommand(RedisKey key, ISerializerSettings config, T objectData, SetOptions options) : base("SET", true, key)
        {
            _configuration = config;
            _objectData = objectData;
            _options = options;
        }

        protected override CommandParameters GetArgs()
        {
            var objStream = _configuration.Serialize(_objectData);

            return new CommandParameters(SetCommand.GetArgs(CommandText, Key, objStream.GetBuffer(), _options), objStream);
        }

        protected override bool TranslateResult(IRedisObject redisObject) => redisObject.IsOk();
    }
}