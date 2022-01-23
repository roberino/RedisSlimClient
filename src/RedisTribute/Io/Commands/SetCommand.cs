using RedisTribute.Types;

namespace RedisTribute.Io.Commands
{
    class SetCommand : RedisCommand<bool>
    {
        readonly byte[] _data;
        readonly SetOptions _options;

        public SetCommand(RedisKey key, byte[] data, SetOptions options = default) : base("SET", true, key)
        {
            _data = data;
            _options = options;
        }

        protected override CommandParameters GetArgs()
        {
            return GetArgs(CommandText, Key, _data, _options);
        }

        public static object[] GetArgs<TData>(string commandText, RedisKey key, TData data, SetOptions options)
        {
            var argCount = 3;

            if (options.Condition != SetCondition.Default)
            {
                argCount++;
            }
            if (options.Expiry.HasValue)
            {
                argCount += 2;
            }

            var args = new object[argCount];

            args[0] = commandText;
            args[1] = key.Bytes;
            args[2] = data!;

            var i = 3;

            if (options.Condition != SetCondition.Default)
            {
                args[i++] = options.Condition == SetCondition.SetKeyIfNotExists ? "NX" : "XX";
            }

            if (options.Expiry.HasValue)
            {
                args[i++] = options.Expiry.Type;
                args[i++] = options.Expiry.IntValue.ToString();
            }

            return args;
        }

        protected override bool TranslateResult(IRedisObject redisObject) => redisObject.IsOk();
    }
}