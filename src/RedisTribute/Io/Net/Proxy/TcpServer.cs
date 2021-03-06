﻿using RedisTribute.Configuration;
using RedisTribute.Io.Server;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.Io.Net.Proxy
{
    sealed class TcpServer : IDisposable
    {
        readonly EndPoint _local;
        readonly EndPoint _target;
        readonly ManualResetEvent _listenWaiter;

        Socket _mainSocket;

        Task _worker;

        CancellationTokenSource _disposed;

        public TcpServer(EndPoint local, EndPoint target = null)
        {
            _local = local;
            _target = target;
            _disposed = new CancellationTokenSource();
            _listenWaiter = new ManualResetEvent(false);
            LocalEndPoint = local;
            ForwardingEndPoint = _target ?? _local;
        }

        public static TcpServer CreateLocalProxy(EndPoint target, int proxyPort)
        {
            return new TcpServer(EndpointUtility.CreateLoopbackEndpoint(proxyPort), target);
        }

        public EndPoint LocalEndPoint { get; }

        public EndPoint ForwardingEndPoint { get; }

        public event Action<Exception> Error;

        public Task StartAsync(RequestHandler handler)
        {
            if (_disposed.IsCancellationRequested) throw new ObjectDisposedException(nameof(TcpServer));

            if (_worker != null) throw new InvalidOperationException("Running!");

            _mainSocket = CreateSocket(_local.AddressFamily);
            _mainSocket.Bind(_local);
            _mainSocket.Listen(10);

            _listenWaiter.Reset();

            _worker = Task.Run(async () => await StartAsync(handler, _disposed.Token), _disposed.Token);

            _listenWaiter.WaitOne();

            return Task.CompletedTask;
        }

        async Task StartAsync(RequestHandler handler, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var _ = Task.Run(async () =>
                    {
                        await Task.Delay(100);

                        _listenWaiter.Set();
                    });

                    var source = await _mainSocket.AcceptAsync();

                    ClientConnection destination;
                    State state;

                    if (_target != null)
                    {
                        destination = new ClientConnection(handler);

                        state = new State(source, destination.DestinationSocket);

                        await destination.ConnectAsync(source, _target);
                    }
                    else
                    {
                        destination = new ClientConnection(handler, source);

                        state = new State(source, destination.DestinationSocket);

                        await destination.ConnectAsync(source);
                    }

                    source.BeginReceive(state.Buffer, 0, state.Buffer.Length, 0, destination.OnDataReceive, state);
                }
                catch (Exception ex)
                {
                    Error?.Invoke(ex);
                    throw;
                }
            }
        }

        class ClientConnection
        {
            readonly RequestHandler _requestHandler;
            readonly CancellationToken _cancellation;

            public ClientConnection(RequestHandler requestHandler, Socket socket = null, CancellationToken cancellation = default)
            {
                DestinationSocket = socket ?? CreateSocket(AddressFamily.InterNetwork);
                _cancellation = cancellation;
                _requestHandler = requestHandler;
            }

            public Socket DestinationSocket { get; }

            public async Task ConnectAsync(Socket destination, EndPoint remoteEndpoint = null)
            {
                var state = new State(DestinationSocket, destination);

                if (remoteEndpoint != null)
                {
                    await DestinationSocket.ConnectAsync(remoteEndpoint);
                }

                DestinationSocket.BeginReceive(state.Buffer, 0, state.Buffer.Length, SocketFlags.None, OnDataReceive, state);
            }

            public void OnDataReceive(IAsyncResult result)
            {
                var state = (State)result.AsyncState;

                try
                {
                    _cancellation.ThrowIfCancellationRequested();

                    var bytesRead = state.SourceSocket.EndReceive(result);

                    if (bytesRead > 0)
                    {
                        var response = _requestHandler.Handle(state.DestinationSocket.RemoteEndPoint, state.Buffer, bytesRead);

                        if (response.Data.Length > 0)
                        {
                            state.DestinationSocket.Send(response.Data, bytesRead, SocketFlags.None);
                            state.SourceSocket.BeginReceive(state.Buffer, 0, state.Buffer.Length, 0, OnDataReceive,
                                state);
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (!_requestHandler.HandleError(ex))
                    {
                        state.DestinationSocket.Close();
                        state.SourceSocket.Close();
                    }
                }
            }
        }

        public void Dispose()
        {
            _listenWaiter.Dispose();
            _disposed.Cancel();
            _mainSocket?.Close();
        }

        static Socket CreateSocket(AddressFamily addressFamily)
        {
            return new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp);
        }

        class State
        {
            public Socket SourceSocket { get; }
            public Socket DestinationSocket { get; }
            public byte[] Buffer { get; }

            public State(Socket source, Socket destination)
            {
                SourceSocket = source;
                DestinationSocket = destination;
                Buffer = new byte[8192];
            }
        }
    }
}