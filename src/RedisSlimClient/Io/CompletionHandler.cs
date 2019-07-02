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
            if (objData.Slice(objData.Length - 2, 1).First.Span[0] != (byte)'\r')
            {
                throw new BufferReadException(objData, null);
            }

            var createdItems = _redisObjectBuilder.AppendObjectData(objData.Slice(0, objData.Length - 2));

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