using NSubstitute;
using RedisTribute.Configuration;
using RedisTribute.Serialization;
using RedisTribute.Types.Graphs;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace RedisTribute.UnitTests.Types.Graphs
{
    public class GraphFactoryTests
    {
        readonly IPersistentDictionary<byte[]> _graphStorage;
        readonly IPersistentDictionaryClient _client;
        readonly ISerializerSettings _serializerSettings;

        public GraphFactoryTests()
        {
            _serializerSettings = Substitute.For<ISerializerSettings>();

            _serializerSettings.Encoding.Returns(Encoding.UTF8);
            _serializerSettings.SerializerFactory.Returns(SerializerFactory.Instance);

            _client = Substitute.For<IPersistentDictionaryClient>();
            _graphStorage = new FakeStore();

            _client.GetHashSetAsync<byte[]>("x", Arg.Any<CancellationToken>()).Returns(_graphStorage);
        }

        [Fact]
        public async Task GetVertexAsync_SomeLabel_CallsPersistentDictionaryClient()
        {
            var factory = new Graph(_client, _serializerSettings);

            var vertex = await factory.GetVertexAsync<string>("x");

            Assert.Equal("x", vertex.Label);
            Assert.Empty(vertex.Edges);
        }

        [Fact]
        public async Task Connect_SomeLabel_AddsEdge()
        {
            var factory = new Graph(_client, _serializerSettings);

            var vertex = await factory.GetVertexAsync<string>("x");

            vertex.Connect("y");

            Assert.Equal("y", vertex.Edges.Single().Label);
        }

        [Fact]
        public async Task Connect_ThenSave_RemoteDataUpdated()
        {
            var factory = new Graph(_client, _serializerSettings);

            var vertex = await factory.GetVertexAsync<string>("x");

            vertex.Connect("y");

            await vertex.SaveAsync();

            var vertex2 = await factory.GetVertexAsync<string>("x");

            Assert.Equal("y", vertex2.Edges.Single().Label);
        }

        class FakeStore : Dictionary<string, byte[]>, IPersistentDictionary<byte[]>
        {
            public int SavedCounter { get; private set; }

            public string Id { get; }

            public Task DeleteAsync(CancellationToken cancellation = default)
            {
                throw new System.NotImplementedException();
            }

            public Task RefreshAsync(CancellationToken cancellation = default)
            {
                throw new System.NotImplementedException();
            }

            public Task SaveAsync(bool forceUpdate = false, CancellationToken cancellation = default)
            {
                SavedCounter++;
                return Task.CompletedTask;
            }

            public Task SaveAsync(System.Func<(string Key, byte[] ProposedValue, byte[] OriginalValue), byte[]> reconcileFunction, CancellationToken cancellation = default)
            {
                SavedCounter++;
                return Task.CompletedTask;
            }
        }
    }
}
