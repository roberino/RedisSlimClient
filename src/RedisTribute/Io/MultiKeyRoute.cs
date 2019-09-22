using RedisTribute.Types;

namespace RedisTribute.Io
{
    class MultiKeyRoute
    {
        public MultiKeyRoute(ICommandExecutor executor, RedisKey[] keys)
        {
            Executor = executor;
            Keys = keys;
        }

        public ICommandExecutor Executor { get; }

        public RedisKey[] Keys { get; }
    }
}