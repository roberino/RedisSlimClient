using RedisSlimClient.Io.Commands;
using RedisSlimClient.Io.Server;
using RedisSlimClient.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RedisSlimClient.Io
{
    class Connection : IConnection
    {
        readonly IServerNodeInitialiser _serverNodeInitialiser;
        readonly SyncronizedInstance<IReadOnlyCollection<IConnectionSubordinate>> _subConnections;

        public Connection(
            IServerNodeInitialiser serverNodeInitialiser)
        {
            _serverNodeInitialiser = serverNodeInitialiser;
            _subConnections = new SyncronizedInstance<IReadOnlyCollection<IConnectionSubordinate>>(_serverNodeInitialiser.InitialiseAsync);
        }

        public async Task<IEnumerable<ICommandExecutor>> RouteCommandAsync(ICommandIdentity command, ConnectionTarget target)
        {
            var connections = await GetAvailableConnections(command, s => target == ConnectionTarget.AllNodes || s == PipelineStatus.Ok || s == PipelineStatus.Uninitialized);

            if (target == ConnectionTarget.AllAvailableMasters)
            {
                return await Select(connections, r => r == ServerRoleType.Master);
            }

            if (target == ConnectionTarget.FirstAvailable)
            {
                return (await Select(connections, _ => true)).Take(1);
            }

            return await Select(connections, _ => true, true);
        }

        public async Task<ICommandExecutor> RouteCommandAsync(ICommandIdentity command)
        {
            var connections = await GetAvailableConnections(command, s => s == PipelineStatus.Ok || s == PipelineStatus.Uninitialized);

            foreach (var match in connections)
            {
                try
                {
                    return await match.GetPipeline();
                }
                catch
                {
                }
            }

            throw new NoAvailableConnectionException();
        }

        public void Dispose()
        {
            _subConnections.Dispose();
        }

        async Task<IEnumerable<ICommandExecutor>> Select(IOrderedEnumerable<IConnectionSubordinate> connections, Func<ServerRoleType, bool> filter, bool includeBroken = false)
        {
            var selectable = await Task.WhenAll(connections.Where(c => filter(c.EndPointInfo.RoleType)).Select(async c =>
            {
                try
                {
                    return await c.GetPipeline();
                }
                catch (Exception ex)
                {
                    if (includeBroken)
                    {
                        return BrokenPipeline.Create(c.EndPointInfo, ex);
                    }

                    return null;
                }
            }));

            return selectable.Where(p => p != null);
        }

        async Task<IOrderedEnumerable<IConnectionSubordinate>> GetAvailableConnections(ICommandIdentity command, Func<PipelineStatus, bool> filter)
        {
            var subConnections = await _subConnections.GetValue();

            return subConnections
                .Where(c => filter(c.Status) && c.EndPointInfo.CanServe(command))
                .OrderBy(c => c.Metrics.Workload);
        }
    }
}