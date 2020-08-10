using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.Util
{
    static class TaskExtensions
    {
        public static void SyncExec(this Task task)
        {
            task.ConfigureAwait(false).GetAwaiter().GetResult();
        }
        public static CancellationTokenAwaiter GetAwaiter(this CancellationToken ct)
        {
            return new CancellationTokenAwaiter(ct);
        }

        public struct CancellationTokenAwaiter : INotifyCompletion, ICriticalNotifyCompletion
        {
            readonly CancellationToken _cancellationToken;

            public CancellationTokenAwaiter(CancellationToken cancellationToken)
            {
                _cancellationToken = cancellationToken;
            }

            public object GetResult()
            {
                if (IsCompleted) throw new OperationCanceledException();

                throw new InvalidOperationException("The cancellation token has not yet been cancelled.");
            }

            public bool IsCompleted => _cancellationToken.IsCancellationRequested;

            public void OnCompleted(Action continuation) =>
                _cancellationToken.Register(continuation);

            public void UnsafeOnCompleted(Action continuation) =>
                _cancellationToken.Register(continuation);
        }
    }
}