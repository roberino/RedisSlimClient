using RedisSlimClient.Configuration;
using RedisSlimClient.Serialization;
using RedisSlimClient.Serialization.Protocol;
using RedisSlimClient.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace RedisSlimClient.Io.Commands
{
    class ObjectGetCommand<T> : IRedisResult<T>
    {
        private readonly string _key;
        private readonly ClientConfiguration _configuration;
        private readonly IObjectSerializer<T> _serializer;
        private readonly TaskCompletionSource<T> _taskCompletionSource;
        public string CommandText => throw new NotImplementedException();

        public ObjectGetCommand(string key, ClientConfiguration config)
        {
            _key = key;
            _configuration = config;
            _serializer = config.SerializerFactory.Create<T>();
            _taskCompletionSource = new TaskCompletionSource<T>();
        }

        public void Complete(RedisObject result)
        {
            if (result is RedisError err)
            {
                _taskCompletionSource.SetException(new RedisServerException(err.Message));
                return;
            }

            if (result is RedisString strData)
            {
                var byteSeq = new ArraySegmentToRedisObjectReader(new StreamIterator(strData.ToStream()));
                var objReader = new ObjectReader(byteSeq, _configuration.Encoding, null, _configuration.SerializerFactory);

                try
                {
                    var obj = _serializer.ReadData(objReader, default);

                    _taskCompletionSource.SetResult(obj);
                }
                catch (Exception ex)
                {
                    _taskCompletionSource.SetException(ex);
                }
            }
        }

        public void Abandon(Exception ex)
        {
            if (ex is TaskCanceledException)
            {
                _taskCompletionSource.SetCanceled();
                return;
            }

            _taskCompletionSource.SetException(ex);
        }

        public void Write(Stream commandWriter)
        {
            commandWriter.Write(GetArgs());
        }

        public TaskAwaiter<T> GetAwaiter() => _taskCompletionSource.Task.GetAwaiter();

        TaskAwaiter IRedisCommand.GetAwaiter() => ((Task)_taskCompletionSource.Task).GetAwaiter();

        public object[] GetArgs() => new object[] { "GET", _key };
    }
}