using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RedisTribute.Io.Commands;

namespace RedisTribute.Io
{
    interface ICommandRouter : IDisposable
    {
        Task<IReadOnlyCollection<MultiKeyRoute>> RouteMultiKeyCommandAsync(IMultiKeyCommandIdentity command);

        Task<IEnumerable<ICommandExecutor>> RouteCommandAsync(ICommandIdentity command, ConnectionTarget target);

        Task<ICommandExecutor> RouteCommandAsync(ICommandIdentity command);
    }
}