using RedisTribute.Io.Pipelines;
using RedisTribute.Io.Scheduling;
using RedisTribute.Serialization.Protocol;
using RedisTribute.Telemetry;
using System;
using System.Buffers;

namespace RedisTribute.Io
{
    class CompletionHandler : ITraceable
    {
        readonly RedisObjectBuilder _redisObjectBuilder;
        readonly ICommandWorkload _commandQueue;
        readonly IWorkScheduler _workScheduler;
        readonly RedisByteSequenceDelimitter _delimiter;

        public CompletionHandler(ICommandWorkload commandQueue, IWorkScheduler workScheduler)
        {
            _commandQueue = commandQueue;
            _workScheduler = workScheduler;
            _redisObjectBuilder = new RedisObjectBuilder();
            _delimiter = new RedisByteSequenceDelimitter();
        }

        public CompletionHandler Attach(IPipelineReceiver receiver)
        {
            receiver.RegisterHandler(_delimiter.Delimit, OnReceive);
            receiver.Error += OnError;
            return this;
        }

        public event Action<(string Action, byte[] Data)> Trace;

        void OnError(Exception ex)
        {
            _commandQueue
                .AbortAll(ex, _workScheduler)
                .ConfigureAwait(false).GetAwaiter().GetResult();
        }

        void OnReceive(ReadOnlySequence<byte> objData)
        {
#if DEBUG1
            if (objData.Length > 0)
            {
                var endByte = objData.Slice((int)(objData.Length - 1), 1).First.Span[0];

                if (endByte != (byte)'\n')
                {
                    throw new ArgumentException(((char)endByte).ToString());
                }
            }
#endif
            var nextData = objData.Length > 2 ? objData.Slice(0, objData.Length - 2) : ReadOnlySequence<byte>.Empty;
            
            var createdItems = _redisObjectBuilder.AppendObjectData(nextData);

            if (createdItems.Length == 0)
            {
                Trace?.Invoke((nameof(_redisObjectBuilder.AppendObjectData), nextData.ToArray()));
            }

            try
            {
                foreach (var item in createdItems)
                {
                    var binding = _commandQueue.BindResult(item);

                    _workScheduler.Schedule(binding);
                }
            }
            catch (Exception ex)
            {
                _commandQueue.AbortAll(ex, _workScheduler).ConfigureAwait(false).GetAwaiter().GetResult();
                throw;
            }
        }
    }
}