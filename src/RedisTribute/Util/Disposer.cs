using System;

namespace RedisTribute.Util
{
    class Disposer : IDisposable
    {
        readonly Action _onDispose;

        bool _disposed;

        public Disposer(Action onDispose)
        {
            _onDispose = onDispose;
        }
        public Disposer(params IDisposable[] disposables) : this(() =>
        {
            foreach(var disposable in disposables)
            {
                disposable.Dispose();
            }
        })
        {
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _onDispose.Invoke();
            }
        }
    }
}
