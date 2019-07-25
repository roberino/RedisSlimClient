using RedisSlimClient.Io.Commands;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RedisSlimClient.Io
{
    internal interface IConnection : IDisposable
    {
        Task<IEnumerable<ICommandExecutor>> RouteCommandAsync(ICommandIdentity command, ConnectionTarget target);
        Task<ICommandExecutor> RouteCommandAsync(ICommandIdentity command);
    }
}