using RedisTribute.Configuration;
using RedisTribute.Io.Commands;
using RedisTribute.Io.Net;
using RedisTribute.Io.Server;
using RedisTribute.Io.Server.Clustering;
using RedisTribute.Telemetry;
using RedisTribute.Util;
using System;
using System.Security.Authentication;
using System.Threading.Tasks;

namespace RedisTribute.Io
{
    class ConnectionSubordinateFactory
    {
        readonly ServerEndPointInfo _initialEndPoint;
        readonly Func<IServerEndpointFactory, Task<ICommandPipeline>> _pipelineFactory;
        readonly ITelemetryWriter _telemetryWriter;
        readonly TimeSpan _timeout;

        public ConnectionSubordinateFactory(
            ServerEndPointInfo endPointInfo,
            IClientCredentials clientCredentials,
            Func<IServerEndpointFactory, Task<ICommandPipeline>> pipelineFactory,
            ITelemetryWriter telemetryWriter, TimeSpan timeout)
        {
            _initialEndPoint = endPointInfo;
            _pipelineFactory = pipelineFactory;
            _telemetryWriter = telemetryWriter;
            _timeout = timeout;

            ClientCredentials = clientCredentials;
        }

        AuthCommand AuthCommand(string password) => new AuthCommand(password).AttachTelemetry(_telemetryWriter);

        ClientSetNameCommand ClientSetName => new ClientSetNameCommand(ClientCredentials.ClientName).AttachTelemetry(_telemetryWriter);

        SelectCommand DatabaseSelectCommand(int index) => new SelectCommand(index).AttachTelemetry(_telemetryWriter);

        public IClientCredentials ClientCredentials { get; }

        public bool IsDefaultDatabase => ClientCredentials.Database == 0;

        public ConnectionSubordinate CreateConnectionSubordinate(ServerEndPointInfo endPointInfo)
        {
            return new ConnectionSubordinate(endPointInfo, new SyncronizedInstance<ICommandPipeline>(async () =>
            {
                try
                {
                    var result = await _telemetryWriter.ExecuteAsync(async ctx =>
                    {
                        ctx.Dimensions[nameof(endPointInfo.Host)] = endPointInfo.Host;
                        ctx.Dimensions[nameof(endPointInfo.Port)] = endPointInfo.Port;
                        ctx.Dimensions[nameof(endPointInfo.MappedPort)] = endPointInfo.MappedPort;

                        var subPipe = await _pipelineFactory(endPointInfo);

                        var password = ClientCredentials.PasswordManager.GetPassword(endPointInfo);

                        int? dbIndex = null;

                        if (ClientCredentials.Database > 0 && !(endPointInfo is ClusterNodeInfo))
                        {
                            dbIndex = ClientCredentials.Database;
                            endPointInfo.SetDatabase(dbIndex.Value);
                        }

                        await Auth(subPipe, password, dbIndex);

                        subPipe.Initialising.Subscribe(p => Auth(p, password, dbIndex));

                        return subPipe;
                    }, nameof(CreateConnectionSubordinate));

                    return result;
                }
                catch (Exception ex)
                {
                    throw new ConnectionInitialisationException(endPointInfo, ex);
                }
            }));
        }

        async Task<ICommandPipeline> Auth(ICommandPipeline pipeline, string password, int? dbIndex)
        {
            if (password != null)
            {
                if (!await pipeline.ExecuteAdminWithTimeout(AuthCommand(password), _timeout))
                {
                    throw new AuthenticationException();
                }
            }

            await pipeline.ExecuteAdminWithTimeout(ClientSetName, _timeout);

            if (dbIndex.HasValue)
            {
                await pipeline.ExecuteAdminWithTimeout(DatabaseSelectCommand(dbIndex.Value), _timeout);
            }
            return pipeline;
        }
    }
}