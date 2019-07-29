using RedisSlimClient.Io.Pipelines;
using RedisSlimClient.Io.Scheduling;
using RedisSlimClient.Serialization.Protocol;
using System;
using System.Buffers;
using System.Threading.Tasks;

namespace RedisSlimClient.Io
{
    class CompletionHandler
    {
        readonly RedisObjectBuilder _redisObjectBuilder;
        readonly IPipelineReceiver _receiver;
        readonly CommandQueue _commandQueue;
        readonly IWorkScheduler _workScheduler;
        readonly RedisByteSequenceDelimitter _delimitter;

        public CompletionHandler(IPipelineReceiver receiver, CommandQueue commandQueue, IWorkScheduler workScheduler)
        {
            _receiver = receiver;
            _commandQueue = commandQueue;
            _workScheduler = workScheduler;
            _redisObjectBuilder = new RedisObjectBuilder();
            _delimitter = new RedisByteSequenceDelimitter();

            _receiver.RegisterHandler(_delimitter.Delimit, OnReceive);
            _receiver.Error += OnError;
        }

        private void OnError(Exception ex)
        {
            _commandQueue.ProcessNextCommand(cmd =>
            {
                _workScheduler.Schedule(() =>
                {
                    cmd.Abandon(ex);
                    return Task.CompletedTask;
                });
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
                    _workScheduler.Schedule(() =>
                    {
                        cmd.Complete(item);
                        return Task.CompletedTask;
                    });
                });
            }
        }
    }
}