using System;

namespace RedisTribute.Io.Commands
{
    readonly struct CommandParameters : IDisposable
    {
        readonly Action _disposer;

        public CommandParameters(Action disposer, params object[] values)
        {
            Values = values;
            _disposer = disposer;
        }

        public CommandParameters(object[] values, Action disposer)
        {
            Values = values;
            _disposer = disposer;
        }

        public object[] Values { get; }

        public static implicit operator CommandParameters(object[] args) => new CommandParameters(null, args);

        public void Dispose()
        {
            _disposer?.Invoke();
        }
    }
}