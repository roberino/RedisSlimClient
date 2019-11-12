using RedisTribute.Types;
using System;

namespace RedisTribute.Io.Commands
{
    class EvalCommand : RedisPrimativeCommand
    {
        readonly string _luaScript;
        readonly RedisKey[] _keys;
        readonly object[] _args;

        public EvalCommand(string luaScript, RedisKey[] keys, params object[] args) : base("EVAL", false)
        {
            _luaScript = luaScript;
            _keys = keys;
            _args = args;
        }

        protected override CommandParameters GetArgs()
        {
            var args = new object[3 + _keys.Length + _args.Length];

            args[0] = CommandText;
            args[1] = _luaScript;
            args[2] = _keys.Length.ToString();

            var i = 3;

            foreach(var k in _keys)
            {
                args[i++] = k.ToString();
            }

            Array.Copy(_args, 0, args, 3 + _keys.Length, _args.Length);

            return args;
        }
    }
}