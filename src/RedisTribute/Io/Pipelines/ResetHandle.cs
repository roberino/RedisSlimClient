using RedisTribute.Util;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.Io.Pipelines
{
    class ResetHandle : IDisposable
    {
        readonly ManualResetEventSlim _resetEvent;

        public ResetHandle()
        {
            _resetEvent = new ManualResetEventSlim(true);
            Resetting = new AsyncEvent<bool>();
        }

        public void Dispose()
        {
            _resetEvent.Dispose();
        }

        public IAsyncEvent<bool> Resetting { get; }

        public bool IsResetting => !_resetEvent.IsSet;

        public event Action Fault;

        public void Enable()
        {
            _resetEvent.Set();
        }

        public async Task<IDisposable> BeginReset()
        {
            _resetEvent.Reset();

            await ((AsyncEvent<bool>)Resetting).PublishAsync(true);

            return new Disposer(() =>
            {
                _resetEvent.Set();
            });
        }

        public void NotifyFault()
        {
            lock (_resetEvent)
            {
                if (_resetEvent.IsSet)
                {
                    _resetEvent.Reset();
                    Fault?.Invoke();
                }
            }
        }

        public async Task AwaitReset()
        {
            while (!_resetEvent.Wait(1000))
            {
                await Task.Delay(100);
            }
        }
    }
}
