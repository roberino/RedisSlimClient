using RedisSlimClient.Io;
using RedisSlimClient.Io.Commands;
using RedisSlimClient.Io.Server;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace RedisSlimClient.UnitTests.Io
{
    public class ConnectionTests
    {
        readonly ITestOutputHelper _output;
        readonly EndPoint _localEndpoint = new Uri("tcp://localhost:8080/").AsEndpoint();

        public ConnectionTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact (Skip = "Flakey")]
        public async Task ConnectAsync_LocalServer_CanConnect()
        {
            using (var server = new TcpServer(_localEndpoint))
            {
                await server.StartAsync(new RequestHandler(x => new Response(x.Data)));

                using (var connection = new Connection(_localEndpoint))
                {
                    var pipeline = await connection.ConnectAsync();

                    Assert.NotNull(pipeline);
                }
            }
        }

        [Fact(Skip = "Flakey")]
        public async Task ConnectAsync_LocalServer_CanPing()
        {
            using (var server = new TcpServer(_localEndpoint))
            {
                await server.StartAsync(new RequestHandler(x =>
                {
                    _output.WriteLine(Encoding.ASCII.GetString(x.Data, 0, x.BytesRead));
                    return new Response(x.Data);
                }));

                using (var connection = new Connection(_localEndpoint))
                {
                    var pipeline = await connection.ConnectAsync();
                    var cmd = new PingCommand();

                    var result = await pipeline.Execute(cmd, TimeSpan.FromMilliseconds(100));

                    Assert.NotNull(result);
                }
            }
        }
    }
}
