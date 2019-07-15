using RedisSlimClient.Io.Server;
using RedisSlimClient.Types;
using System.Threading.Tasks;
using Xunit;

namespace RedisSlimClient.UnitTests.Io.Server
{
    public class InfoCommandTests
    {
        [Theory]
        [InlineData("Stats", "total_net_input_bytes", 589753126)]
        [InlineData("Memory", "used_memory", 114469824)]
        [InlineData("Clients", "blocked_clients", 0)]
        public async Task Complete_GetLongValue_ReturnsExpectedValue(string section, string key, long expectedValue)
        {
            var cmd = new InfoCommand();

            var str = new RedisString("InfoResponse.txt".OpenBinaryResourceBytes());

            cmd.Complete(str);

            var result = await cmd;

            var actualValue = result[section][key];

            Assert.Equal(expectedValue, actualValue);
        }

        [Theory]
        [InlineData("Server", "executable", "/usr/local/bin/redis-server")]
        [InlineData("Persistence", "rdb_last_bgsave_status", "ok")]
        public async Task Complete_GetstringValue_ReturnsExpectedValue(string section, string key, string expectedValue)
        {
            var cmd = new InfoCommand();

            var str = new RedisString("InfoResponse.txt".OpenBinaryResourceBytes());

            cmd.Complete(str);

            var result = await cmd;

            var actualValue = result[section][key];

            Assert.Equal(expectedValue, actualValue);
        }
    }
}
