using NSubstitute;
using RedisTribute.Configuration;
using RedisTribute.Serialization;
using RedisTribute.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace RedisTribute.UnitTests.Types
{
    public class RedisHashSetTests
    {
        readonly IHashSetClient _client;
        readonly ISerializerSettings _settings;

        public RedisHashSetTests()
        {
            _client = Substitute.For<IHashSetClient>();
            _settings = Substitute.For<ISerializerSettings>();

            _settings.Encoding.Returns(Encoding.UTF8);
            _settings.SerializerFactory.Returns(SerializerFactory.Instance);
        }

        [Fact]
        public async Task CreateAsync_GivenSomeClient_FetchesHashsetData()
        {
            _client.GetAllHashFieldsAsync("x", Arg.Any<CancellationToken>()).Returns(new Dictionary<string, byte[]>()
            {
                ["a"] = Encoding.UTF8.GetBytes("123"),
                ["b"] = Encoding.UTF8.GetBytes("456"),
            });

            var hashset = await RedisHashSet<string>.CreateAsync("x", _client, _settings);

            Assert.Equal("123", hashset["a"]);
        }

        [Fact]
        public async Task SaveAsync_GivenSomeClient_SendDataToClient()
        {
            _client.GetAllHashFieldsAsync("x", Arg.Any<CancellationToken>()).Returns(new Dictionary<string, byte[]>());

            var hashset = await RedisHashSet<string>.CreateAsync("x", _client, _settings);

            hashset["x1"] = "abc";
            hashset["y1"] = "def";

            await hashset.SaveAsync();

            await _client.Received().SetHashFieldAsync("x", "x1", Arg.Is<byte[]>(d => AreEqual("abc", d)));
            await _client.Received().SetHashFieldAsync("x", "y1", Arg.Is<byte[]>(d => AreEqual("def", d)));
        }

        [Fact]
        public async Task SaveAsync_DataHasChanged_ReconcileFunctionInvoked()
        {
            _client.GetAllHashFieldsAsync("x", Arg.Any<CancellationToken>()).Returns(new Dictionary<string, byte[]>()
            {
                ["a"] = Encoding.UTF8.GetBytes("123"),
                ["b"] = Encoding.UTF8.GetBytes("456"),
            });

            var hashset = await RedisHashSet<string>.CreateAsync("x", _client, _settings);

            hashset["a"] = "xxx";

            _client.GetAllHashFieldsAsync("x", Arg.Any<CancellationToken>()).Returns(new Dictionary<string, byte[]>()
            {
                ["a"] = Encoding.UTF8.GetBytes("678"),
                ["b"] = Encoding.UTF8.GetBytes("456"),
            });

            await hashset.SaveAsync(x => x.Key == "a" ? "?" : "");

            Assert.Equal("?", hashset["a"]);
        }

        [Fact]
        public async Task SaveAsync_DataHasChangedButForceSetToTrue_RemoteValueOverwritten()
        {
            _client.GetAllHashFieldsAsync("x", Arg.Any<CancellationToken>()).Returns(new Dictionary<string, byte[]>()
            {
                ["a"] = Encoding.UTF8.GetBytes("123"),
                ["b"] = Encoding.UTF8.GetBytes("456"),
            });

            var hashset = await RedisHashSet<string>.CreateAsync("x", _client, _settings);

            hashset["a"] = "xxx";

            _client.GetAllHashFieldsAsync("x", Arg.Any<CancellationToken>()).Returns(new Dictionary<string, byte[]>()
            {
                ["a"] = Encoding.UTF8.GetBytes("678"),
                ["b"] = Encoding.UTF8.GetBytes("456"),
            });

            await hashset.SaveAsync(forceUpdate: true);

            Assert.Equal("xxx", hashset["a"]);

            await _client.Received(1).SetHashFieldAsync("x", "a", Arg.Is<byte[]>(d => AreEqual("xxx", d)));
        }

        [Fact]
        public async Task SaveAsync_DataHasChangedButNewDataIsEqual_ReconcileFunctionNotInvoked()
        {
            _client.GetAllHashFieldsAsync("x", Arg.Any<CancellationToken>()).Returns(new Dictionary<string, byte[]>()
            {
                ["a"] = Encoding.UTF8.GetBytes("123"),
                ["b"] = Encoding.UTF8.GetBytes("456"),
            });

            var hashset = await RedisHashSet<string>.CreateAsync("x", _client, _settings);

            hashset["a"] = "678";

            _client.GetAllHashFieldsAsync("x", Arg.Any<CancellationToken>()).Returns(new Dictionary<string, byte[]>()
            {
                ["a"] = Encoding.UTF8.GetBytes("678"),
                ["b"] = Encoding.UTF8.GetBytes("456"),
            });

            await hashset.SaveAsync(x => throw new InvalidOperationException());

            Assert.Equal("678", hashset["a"]);
        }

        [Fact]
        public async Task SaveAsync_AddNewValue_SetsNewValue()
        {
            _client.GetAllHashFieldsAsync("x", Arg.Any<CancellationToken>()).Returns(new Dictionary<string, byte[]>()
            {
                ["a"] = Encoding.UTF8.GetBytes("123"),
                ["b"] = Encoding.UTF8.GetBytes("456"),
            });

            var hashset = await RedisHashSet<string>.CreateAsync("x", _client, _settings);

            hashset["c"] = "678";

            await hashset.SaveAsync(x => throw new InvalidOperationException());

            Assert.Equal("678", hashset["c"]);

            await _client.Received(1).SetHashFieldAsync("x", "c", Arg.Is<byte[]>(d => AreEqual("678", d)));
            await _client.DidNotReceive().SetHashFieldAsync("x", "a", Arg.Any<byte[]>());
            await _client.DidNotReceive().SetHashFieldAsync("x", "b", Arg.Any<byte[]>());
        }

        [Fact]
        public async Task SaveAsync_DeleteValue_SetsNull()
        {
            _client.GetAllHashFieldsAsync("x", Arg.Any<CancellationToken>()).Returns(new Dictionary<string, byte[]>()
            {
                ["a"] = Encoding.UTF8.GetBytes("123"),
                ["b"] = Encoding.UTF8.GetBytes("456"),
            });

            var hashset = await RedisHashSet<string>.CreateAsync("x", _client, _settings);

            Assert.Equal(2, hashset.Count);

            hashset.Remove("a");

            Assert.Single(hashset);
            Assert.DoesNotContain(hashset.Keys, k => k == "a");
            Assert.DoesNotContain(hashset, k => k.Key == "a");
            Assert.Throws<KeyNotFoundException>(() => hashset["a"]);

            await hashset.SaveAsync(x => throw new InvalidOperationException());

            await _client.Received(1).SetHashFieldAsync("x", "a", null);

            Assert.DoesNotContain(hashset.Keys, k => k == "a");
            Assert.DoesNotContain(hashset, k => k.Key == "a");
            Assert.Throws<KeyNotFoundException>(() => hashset["a"]);
        }

        [Fact]
        public async Task DeleteAsync_SomeValues_SetsNullOnEachAndRemovesAllItemsFromDictionary()
        {
            _client.GetAllHashFieldsAsync("x", Arg.Any<CancellationToken>()).Returns(new Dictionary<string, byte[]>()
            {
                ["a"] = Encoding.UTF8.GetBytes("123"),
                ["b"] = Encoding.UTF8.GetBytes("456"),
            });

            var hashset = await RedisHashSet<string>.CreateAsync("x", _client, _settings);

            hashset["c"] = "678";

            await hashset.DeleteAsync();

            await _client.Received(1).SetHashFieldAsync("x", "a", null);
            await _client.Received(1).SetHashFieldAsync("x", "b", null);

            Assert.Empty(hashset);
        }

        bool AreEqual(string value, byte[] encodedValue)
        {
            return StructuralComparisons.StructuralEqualityComparer.Equals(encodedValue, Encoding.UTF8.GetBytes(value));
        }
    }
}
