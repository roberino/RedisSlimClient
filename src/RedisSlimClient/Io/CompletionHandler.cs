using RedisSlimClient.Io.Pipelines;
using RedisSlimClient.Serialization.Protocol;
using System;
using System.Buffers;
using System.Threading;

namespace RedisSlimClient.Io
{
    class CompletionHandler
    {
        readonly RedisObjectBuilder _redisObjectBuilder;
        readonly IPipelineReceiver _receiver;
        readonly CommandQueue _commandQueue;
        readonly RedisByteSequenceDelimitter _delimitter;

        public CompletionHandler(IPipelineReceiver receiver, CommandQueue commandQueue)
        {
            _receiver = receiver;
            _commandQueue = commandQueue;
            _redisObjectBuilder = new RedisObjectBuilder();
            _delimitter = new RedisByteSequenceDelimitter();

            _receiver.RegisterHandler(_delimitter.Delimit, OnReceive);
            _receiver.Error += OnError;
        }

        private void OnError(Exception ex)
        {
            _commandQueue.ProcessNextCommand(cmd =>
            {
                ThreadPool.QueueUserWorkItem(_ => cmd.Abandon(ex));
            });
        }

        private void OnReceive(ReadOnlySequence<byte> objData)
        {
            var nextData = objData.Slice(0, objData.Length - 2);
            
            var createdItems = _redisObjectBuilder.AppendObjectData(nextData);

            foreach (var item in createdItems)
            {
                _commandQueue.ProcessNextCommand(cmd =>
                {
                    ThreadPool.QueueUserWorkItem(_ => cmd.Complete(item));
                });
            }
        }
    }
}