using RedisTribute.Configuration;
using RedisTribute.Serialization;
using RedisTribute.Util;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.Types
{
    class RedisHashSet<T> : IPersistentDictionary<T>
    {
        readonly ConcurrentDictionary<string, ValueState> _values;
        readonly IHashSetClient _client;
        readonly ISerializerSettings _serializerSettings;
        readonly IAsyncLockStrategy<IAsyncLock> _lockStrategy;
        readonly IObjectSerializer<T> _serializer;
        readonly AsyncLock _lock;

        internal RedisHashSet(RedisKey key,
            IHashSetClient client,
            ISerializerSettings serializerSettings,
            IDictionary<string, byte[]> originalValues = null,
            IAsyncLockStrategy<IAsyncLock> lockStrategy = null)
        {
            Key = key;

            _lock = new AsyncLock();
            _client = client;
            _serializerSettings = serializerSettings;
            _lockStrategy = lockStrategy ?? new LocalLockStrategy(TimeSpan.FromSeconds(5));
            _serializer = serializerSettings.SerializerFactory.Create<T>();

            _values = new ConcurrentDictionary<string, ValueState>();

            if (originalValues != null)
            {
                foreach (var kv in originalValues)
                {
                    var ov = serializerSettings.Deserialize(_serializer, kv.Value);
                    _values[kv.Key] = new ValueState(ov);
                }
            }
        }

        public static async Task<RedisHashSet<T>> CreateAsync(RedisKey key, 
            IHashSetClient client, ISerializerSettings serializerSettings, 
            IAsyncLockStrategy<IAsyncLock> lockStrategy = null, CancellationToken cancellation = default)
        {
            var hashSet = new RedisHashSet<T>(key, client, serializerSettings, lockStrategy: lockStrategy);

            await hashSet.RefreshAsync(cancellation);

            return hashSet;
        }

        public string Id => Key.ToString();

        public T this[string key]
        {
            get
            {
                var v = _values[key];

                if (v.IsDeleted)
                {
                    throw new KeyNotFoundException(key);
                }

                return v.Value;
            }
            set => Add(key, value);
        }

        public RedisKey Key { get; }
        public ICollection<string> Keys => _values.Where(v => !v.Value.IsDeleted).Select(v => v.Key).ToList();
        public ICollection<T> Values => _values.Values.Where(v => !v.IsDeleted).Select(v => v.Value).ToList();
        public int Count => Keys.Count;
        public bool IsReadOnly => false;
        public void Add(string key, T value)
        {
            _values.AddOrUpdate(key, new ValueState
            {
                Value = value
            }, (k, v) =>
            {
                v.Value = value;
                return v;
            });
        }

        public void Add(KeyValuePair<string, T> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            foreach (var item in _values.Values)
            {
                item.Delete();
            }
        }

        public bool Contains(KeyValuePair<string, T> item) => _values.Any(v => v.Key == item.Key && v.Value.Value.Equals(item.Value));

        public bool ContainsKey(string key) => _values.ContainsKey(key);

        public void CopyTo(KeyValuePair<string, T>[] array, int arrayIndex)
        {
            var i = arrayIndex;

            foreach (var item in _values)
            {
                array[i++] = new KeyValuePair<string, T>(item.Key, item.Value.Value);

                if (i == array.Length)
                {
                    break;
                }
            }
        }

        public IEnumerator<KeyValuePair<string, T>> GetEnumerator() => _values
            .Where(v => !v.Value.IsDeleted)
            .Select(v => new KeyValuePair<string, T>(v.Key, v.Value.Value))
            .GetEnumerator();

        public bool Remove(string key)
        {
            if (_values.TryGetValue(key, out var state))
            {
                state.Delete();
                return true;
            }

            return false;
        }

        public bool Remove(KeyValuePair<string, T> item)
        {
            return Remove(item.Key);
        }

        public async Task SaveAsync(bool forceUpdate = false, CancellationToken cancellation = default)
        {
            if (forceUpdate)
            {
                await SaveAsync(false, x => x.ProposedValue, cancellation);
                return;
            }

            await SaveAsync(_ => throw new ArgumentException("Data"), cancellation);
        }

        public Task SaveAsync(Func<(string Key, T ProposedValue, T OriginalValue), T> reconcileFunction, CancellationToken cancellation = default)
            => SaveAsync(true, reconcileFunction, cancellation);

        public bool TryGetValue(string key, out T value)
        {
            if (_values.TryGetValue(key, out var v))
            {
                value = v.Value;
                return true;
            }
            value = default;
            return false;
        }

        public async Task RefreshAsync(CancellationToken cancellation = default)
        {
            using (await _lockStrategy.AquireLockAsync($"$$_HashLock:{Id}", cancellation: cancellation))
            {
                var originalValues = await _client.GetAllHashFieldsAsync(Key.ToString(), cancellation);

                foreach (var item in _values)
                {
                    if (!item.Value.IsDirty && !originalValues.ContainsKey(item.Key))
                    {
                        _values.TryRemove(item.Key, out _);
                    }
                }

                foreach (var item in originalValues)
                {
                    if (_values.TryGetValue(item.Key, out var state))
                    {
                        if (!state.IsDirty)
                        {
                            var valueObj = _serializerSettings.Deserialize(_serializer, item.Value);
                            _values[item.Key] = new ValueState(valueObj);
                        }
                    }
                    else
                    {
                        var valueObj = _serializerSettings.Deserialize(_serializer, item.Value);
                        _values[item.Key] = new ValueState(valueObj);
                    }
                }
            }
        }

        public async Task DeleteAsync(CancellationToken cancellation = default)
        {
            var remoteValues = await _client.GetAllHashFieldsAsync(Key.ToString(), cancellation);

            var deleteTasks = remoteValues.Select(async item =>
                 {
                     await _client.SetHashFieldAsync(Key.ToString(), item.Key, null);

                     _values.TryRemove(item.Key, out _);
                 }
              );

            await Task.WhenAll(deleteTasks);

            _values.Clear();
        }

        public void RevertChanges()
        {
            foreach(var x in _values.Where(v => v.Value.IsDirty))
            {
                x.Value.Revert();
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        class ValueState
        {
            T _newValue;

            public ValueState(T originalValue)
            {
                OriginalValue = originalValue;
            }

            public ValueState()
            {
                IsNew = true;
            }

            public bool IsNew { get; private set; }

            public bool IsDeleted { get; private set; }

            public T OriginalValue { get; private set; }

            public T Value
            {
                get => IsDirty ? _newValue : OriginalValue;
                set
                {
                    _newValue = value;
                    IsDirty = true;
                }
            }

            public void Delete()
            {
                IsDeleted = true;
                IsDirty = true;
            }

            public void WasUpdated()
            {
                OriginalValue = _newValue;
                _newValue = default;
                IsDirty = false;
                IsDeleted = false;
                IsNew = false;
            }

            public void Revert()
            {
                if (!IsNew)
                {
                    _newValue = default;
                    IsDeleted = false;
                    IsDirty = false;
                }
            }

            public bool IsDirty { get; private set; }
        }

        async Task SaveAsync(bool checkOriginal, Func<(string Key, T ProposedValue, T OriginalValue), T> reconcileFunction, CancellationToken cancellation = default)
        {
            using (await _lock.LockAsync())
            {
                var originalValues = checkOriginal ? await _client.GetAllHashFieldsAsync(Key.ToString(), cancellation) : new Dictionary<string, byte[]>();
                var newData = new Dictionary<string, (ValueState State, byte[] Data, T Proposed, bool WasReconciled)>();

                foreach (var item in _values)
                {
                    if (item.Value.IsDirty)
                    {
                        var reconciled = false;
                        var proposedValue = item.Value.Value;

                        if (checkOriginal && originalValues.TryGetValue(item.Key, out var originalValueBytes))
                        {
                            var originalValue = _serializerSettings.Deserialize(_serializer, originalValueBytes);

                            if (item.Value.IsNew)
                            {
                                if (!_serializerSettings.AreBinaryEqual(originalValueBytes, proposedValue))
                                {
                                    proposedValue = reconcileFunction((item.Key, proposedValue, originalValue));
                                    reconciled = true;
                                }
                            }
                            else
                            {
                                var eq = _serializerSettings.AreBinaryEqual(originalValueBytes, item.Value.OriginalValue);

                                if (!eq)
                                {
                                    if (item.Value.IsDeleted)
                                    {
                                        proposedValue = reconcileFunction((item.Key, default, originalValue));

                                        if (proposedValue != default)
                                        {
                                            reconciled = true;
                                        }
                                    }
                                    else
                                    {
                                        if (!_serializerSettings.AreBinaryEqual(originalValueBytes, proposedValue))
                                        {
                                            proposedValue = reconcileFunction((item.Key, proposedValue, originalValue));
                                            reconciled = true;
                                        }
                                    }
                                }
                            }
                        }

                        var data = item.Value.IsDeleted && !reconciled ? null : _serializerSettings.SerializeAsBytes(proposedValue);

                        newData[item.Key] = (item.Value, data, proposedValue, reconciled);

                        cancellation.ThrowIfCancellationRequested();
                    }
                }

                var updateTasks = newData.Select(async item =>
                {
                    await _client.SetHashFieldAsync(Key.ToString(), item.Key, item.Value.Data, cancellation);

                    if (item.Value.WasReconciled)
                    {
                        item.Value.State.Value = item.Value.Proposed;
                    }
                    else
                    {
                        if (item.Value.State.IsDeleted)
                        {
                            _values.TryRemove(item.Key, out _);
                        }
                    }

                    item.Value.State.WasUpdated();
                });

                await Task.WhenAll(updateTasks);
            }
        }
    }
}