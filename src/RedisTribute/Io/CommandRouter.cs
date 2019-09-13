using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RedisTribute.Io.Commands;
using RedisTribute.Io.Server;
using RedisTribute.Io.Server.Clustering;
using RedisTribute.Util;

namespace RedisTribute.Io
{
    class CommandRouter : ICommandRouter
    {
        readonly IServerNodeInitialiser _serverNodeInitialiser;
        readonly SyncronizedInstance<IReadOnlyCollection<IConnectionSubordinate>> _subConnections;

        public CommandRouter(
            IServerNodeInitialiser serverNodeInitialiser)
        {
            _serverNodeInitialiser = serverNodeInitialiser;
            _subConnections = new SyncronizedInstance<IReadOnlyCollection<IConnectionSubordinate>>(_serverNodeInitialiser.InitialiseAsync);

            _serverNodeInitialiser.ConfigurationChanged += () => _subConnections.Reset().SyncExec();
        }

        public async Task<IReadOnlyCollection<MultiKeyRoute>> RouteMultiKeyCommandAsync(IMultiKeyCommandIdentity command)
        {
            var subConnections = (await _subConnections.GetValue())
                .Where(s => s.Status == PipelineStatus.Ok || s.Status == PipelineStatus.Uninitialized)
                .ToList();

            if (!subConnections.Any(c => c.EndPointInfo.IsCluster))
            {
                var pipe = await subConnections.OrderBy(x => x.Metrics.Workload).First().GetPipeline();

                return new[] {new MultiKeyRoute(pipe, command.Keys.ToArray())};
            }

            var groups = command.Keys.GroupBy(k => HashGenerator.Generate(k)).ToArray();
            var results = new MultiKeyRoute[groups.Length];
            var i = 0;

            foreach (var keyGroup in groups)
            {
                var conn = subConnections.FirstOrDefault(s => s.EndPointInfo.CanServe(command, keyGroup.First()));

                if (conn == null)
                {
                    throw new NoAvailableConnectionException();
                }

                var executor = await conn.GetPipeline();

                results[i++] = new MultiKeyRoute(executor, keyGroup.ToArray());
            }

            return results;
        }

        public async Task<IEnumerable<ICommandExecutor>> RouteCommandAsync(ICommandIdentity command, ConnectionTarget target)
        {
            var connections = (await GetAvailableConnections(command, s => target == ConnectionTarget.AllNodes || s == PipelineStatus.Ok || s == PipelineStatus.Uninitialized))
                .OrderBy(x => HealthPriority(x.Status));

            switch (target)
            {
                case ConnectionTarget.AllAvailableNodes:
                    return await Select(connections, _ => true);
                case ConnectionTarget.AllAvailableMasters:
                    return await Select(connections, r => r == ServerRoleType.Master);
                case ConnectionTarget.FirstAvailable:
                    return (await Select(connections, _ => true)).Take(1);
                default:
                    return await Select(connections, _ => true, true);
            }
        }

        public async Task<ICommandExecutor> RouteCommandAsync(ICommandIdentity command)
        {
            var connections = await GetAvailableConnections(command, s => true);

            foreach (var match in connections.OrderBy(x => HealthPriority(x.Status)))
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

        int HealthPriority(PipelineStatus status)
        {
            switch (status)
            {
                case PipelineStatus.Broken: return 2;
                case PipelineStatus.Reinitializing: return 1;
                case PipelineStatus.Ok:
                case PipelineStatus.Uninitialized:
                    return 0;
            }

            return 100;
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
                .Where(c => c.Status != PipelineStatus.Disabled && filter(c.Status) && c.EndPointInfo.CanServe(command))
                .OrderBy(c => c.Metrics.Workload);
        }
    }
}