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
using Xunit.Abstractions;

namespace RedisTribute.UnitTests.Types.Graphs
{
    public class GraphFactoryTests
    {
        readonly IPersistentDictionary<byte[]> _graphStorage;
        readonly IPersistentDictionaryProvider _client;
        readonly ISerializerSettings _serializerSettings;
        readonly ITestOutputHelper _output;

        public GraphFactoryTests(ITestOutputHelper output)
        {
            _serializerSettings = Substitute.For<ISerializerSettings>();

            _serializerSettings.Encoding.Returns(Encoding.UTF8);
            _serializerSettings.SerializerFactory.Returns(SerializerFactory.Instance);

            _client = Substitute.For<IPersistentDictionaryProvider>();
            _graphStorage = new FakeStore();

            _client.GetHashSetAsync<byte[]>("graph://default/vertex/x", Arg.Any<CancellationToken>()).Returns(_graphStorage);
            _output = output;
        }

        [Fact]
        public async Task GetVertexAsync_SomeLabel_CallsPersistentDictionaryClient()
        {
            var factory = new Graph<string>(_client, _serializerSettings);

            var vertex = await factory.GetVertexAsync("x");

            Assert.Equal("x", vertex.Id);
            Assert.Empty(vertex.Edges);
        }

        [Fact]
        public async Task Connect_SomeLabel_AddsEdge()
        {
            var factory = new Graph<string>(_client, _serializerSettings);

            var vertex = await factory.GetVertexAsync("x");

            vertex.Connect("y");

            Assert.Equal("y", vertex.Edges.Single().TargetVertex.Id);
        }

        [Fact]
        public async Task Connect_ThenSave_RemoteDataUpdated()
        {
            var factory = new Graph<string>(_client, _serializerSettings);

            var vertex = await factory.GetVertexAsync("x");

            vertex.Connect("y");

            await vertex.SaveAsync();

            var vertex2 = await factory.GetVertexAsync("x");

            Assert.Equal("y", vertex2.Edges.Single().TargetVertex.Id);
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

            public void RevertChanges()
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
