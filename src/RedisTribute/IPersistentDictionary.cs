using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute
{
    public interface IPersistentDictionary<T> : IDictionary<string, T>
    {
        Task RefreshAsync(CancellationToken cancellation = default);
        Task SaveAsync(bool forceUpdate = false, CancellationToken cancellation = default);
        Task SaveAsync(Func<(string Key, T ProposedValue, T OriginalValue), T> reconcileFunction, CancellationToken cancellation = default);
    }
}