using System;

namespace RedisTribute.Io.Commands
{
    readonly struct CommandParameters : IDisposable
    {
        readonly IDisposable _disposer;

        public CommandParameters(IDisposable disposer, params object[] values)
        {
            Values = values;
            _disposer = disposer;
        }

        public CommandParameters(object[] values, IDisposable disposer)
        {
            Values = values;
            _disposer = disposer;
        }

        public object[] Values { get; }

        public static implicit operator CommandParameters(object[] args) => new CommandParameters(null, args);

        public void Dispose()
        {
            _disposer?.Dispose();
        }
    }
}