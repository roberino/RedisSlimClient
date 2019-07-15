using RedisSlimClient.Io.Commands;
using RedisSlimClient.Io.Server.Clustering;
using Xunit;

namespace RedisSlimClient.UnitTests.Io.Server.Clustering
{
    public class HashGeneratorTests
    {
        [Theory]
        [InlineData("abc", 7638)]
        [InlineData("xyz", 7349)]
        [InlineData("Xyz", 6771)]
        [InlineData("987234_mjsdf", 16284)]
        [InlineData("xPhj99{01}Kq", 9191)]
        [InlineData("kkfn{01}Pp", 9191)]
        [InlineData("{}abc", 5980)]
        public void Generate_SomePlainString_ReturnsCorrectHash(string key, long expected)
        {
            var hash = HashGenerator.Generate(key);

            Assert.Equal(expected, hash);
        }
    }
}