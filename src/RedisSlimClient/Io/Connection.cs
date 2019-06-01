using RedisSlimClient.Util;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace RedisSlimClient.Io
{
    internal class Connection : IConnection
    {
        static readonly SyncedCounter IdGenerator = new SyncedCounter();

        readonly EndPoint _endPoint;
        readonly Socket _socket;
        readonly AsyncLock<ICommandPipeline> _pipeline;

        public Connection(EndPoint endPoint, Func<Socket, ICommandPipeline> pipelineFactory)
        {
            _endPoint = endPoint;
            _socket = new Socket(_endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            _pipeline = new AsyncLock<ICommandPipeline>(async () =>
            {
                await _socket.ConnectAsync(_endPoint);

                return pipelineFactory(_socket);
            });

            Id = IdGenerator.Increment().ToString();
        }

        public float WorkLoad => 1f;

        public string Id { get; }

        public async Task<ICommandPipeline> ConnectAsync()
        {
            return await _pipeline.GetValue();
        }

        public void Dispose()
        {
            _socket.Dispose();
            _pipeline.Dispose();
        }
    }
}
