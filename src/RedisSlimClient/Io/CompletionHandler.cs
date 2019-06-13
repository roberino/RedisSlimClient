using RedisSlimClient.Io.Pipelines;
using RedisSlimClient.Serialization.Protocol;
using System.Buffers;

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
        }

        private void OnReceive(ReadOnlySequence<byte> obj)
        {
            var createdItems = _redisObjectBuilder.AppendObjectData(obj);

            foreach(var item in createdItems)
            {
                _commandQueue.ProcessNextCommand(cmd =>
                {
                    cmd.Complete(item);
                });
            }
        }
    }
}