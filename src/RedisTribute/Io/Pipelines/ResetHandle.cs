using RedisTribute.Util;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.Io.Pipelines
{
    class ResetHandle : IDisposable
    {
        readonly ManualResetEvent _resetEvent;
        volatile bool _resetting;

        public ResetHandle()
        {
            _resetEvent = new ManualResetEvent(true);
            Resetting = new AsyncEvent<bool>();
        }

        public void Dispose()
        {
            _resetEvent.Dispose();
        }

        public IAsyncEvent<bool> Resetting { get; }

        public bool IsResetting => _resetting;

        public event Action Fault;

        public void Enable()
        {
            _resetEvent.Set();
        }

        public async Task<IDisposable> BeginReset()
        {
            _resetEvent.Reset();
            _resetting = true;

            await ((AsyncEvent<bool>)Resetting).PublishAsync(true);

            return new Disposer(() =>
            {
                _resetEvent.Set();
                _resetting = false;
            });
        }

        public void NotifyFault()
        {
            _resetEvent.Reset();
            _resetting = true;
            Fault?.Invoke();
        }

        public async Task AwaitReset()
        {
            while (!_resetEvent.WaitOne(1000))
            {
                await Task.Delay(100);
            }
        }
    }
}
