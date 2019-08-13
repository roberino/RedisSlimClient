using RedisSlimClient.Io.Commands;
using RedisSlimClient.Types;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RedisSlimClient.Io
{
    interface ICommandRouter : IDisposable
    {
        Task<IDictionary<ICommandExecutor, IList<RedisKey>>> RouteMultiKeyCommandAsync(IMultiKeyCommandIdentity command);

        Task<IEnumerable<ICommandExecutor>> RouteCommandAsync(ICommandIdentity command, ConnectionTarget target);

        Task<ICommandExecutor> RouteCommandAsync(ICommandIdentity command);
    }
}