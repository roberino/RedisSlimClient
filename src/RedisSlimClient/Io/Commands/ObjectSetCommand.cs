using RedisSlimClient.Configuration;
using RedisSlimClient.Serialization;
using RedisSlimClient.Serialization.Protocol;
using RedisSlimClient.Types;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace RedisSlimClient.Io.Commands
{
    class ObjectSetCommand<T> : IRedisResult<bool>
    {
        private readonly string _key;
        private readonly ClientConfiguration _configuration;
        private readonly T _objectData;
        private readonly IObjectSerializer<T> _serializer;
        private readonly TaskCompletionSource<bool> _taskCompletionSource;
        public string CommandText => throw new NotImplementedException();

        public ObjectSetCommand(string key, ClientConfiguration config, T objectData)
        {
            _key = key;
            _configuration = config;
            _objectData = objectData;
            _serializer = config.SerializerFactory.Create<T>();
            _taskCompletionSource = new TaskCompletionSource<bool>();
        }

        public void Complete(RedisObject result)
        {
            try
            {
                _taskCompletionSource.SetResult(string.Equals(result.ToString(), "OK", StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                _taskCompletionSource.SetException(ex);
            }
        }

        public void Abandon(Exception ex)
        {
            if (ex is TaskCanceledException)
            {
                Cancel();
                return;
            }

            _taskCompletionSource.SetException(ex);
        }
        public void Cancel()
        {
            _taskCompletionSource.SetCanceled();
        }

        public void Write(Stream commandWriter)
        {
            commandWriter.WriteStartArray(3);
            commandWriter.Write("SET", true);
            commandWriter.Write(_key, true);
            commandWriter.Write(GetObjectData());
        }

        public TaskAwaiter<bool> GetAwaiter() => _taskCompletionSource.Task.GetAwaiter();

        TaskAwaiter IRedisCommand.GetAwaiter() => ((Task)_taskCompletionSource.Task).GetAwaiter();

        public object[] GetArgs() => new object[] { "SET", _key, GetObjectData() };

        byte[] GetObjectData()
        {
            using (var ms = new MemoryStream())
            {
                var objWriter = new ObjectWriter(ms, _configuration.Encoding, null, _configuration.SerializerFactory);

                _serializer.WriteData(_objectData, objWriter);

                return ms.ToArray();
            }
        }
    }
}