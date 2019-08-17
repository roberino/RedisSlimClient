﻿using RedisSlimClient.Io.Pipelines;
using RedisSlimClient.Io.Scheduling;
using RedisSlimClient.Serialization.Protocol;
using System;
using System.Buffers;
using System.Threading.Tasks;
using RedisSlimClient.Telemetry;

namespace RedisSlimClient.Io
{
    class CompletionHandler : ITraceable
    {
        readonly RedisObjectBuilder _redisObjectBuilder;
        readonly IPipelineReceiver _receiver;
        readonly CommandQueue _commandQueue;
        readonly IWorkScheduler _workScheduler;
        readonly RedisByteSequenceDelimitter _delimiter;

        public CompletionHandler(IPipelineReceiver receiver, CommandQueue commandQueue, IWorkScheduler workScheduler)
        {
            _receiver = receiver;
            _commandQueue = commandQueue;
            _workScheduler = workScheduler;
            _redisObjectBuilder = new RedisObjectBuilder();
            _delimiter = new RedisByteSequenceDelimitter();

            _receiver.RegisterHandler(_delimiter.Delimit, OnReceive);
            _receiver.Error += OnError;
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
            var nextData = objData.Slice(0, objData.Length - 2);
            
            var createdItems = _redisObjectBuilder.AppendObjectData(nextData);

            if (createdItems.Length == 0)
            {
                Trace?.Invoke((nameof(_redisObjectBuilder.AppendObjectData), nextData.ToArray()));
            }

            foreach (var item in createdItems)
            {
                if (!_commandQueue.ProcessNextCommand(cmd =>
                {
                    _workScheduler.Schedule(() =>
                    {
                        cmd.Complete(item);
                        return Task.CompletedTask;
                    });
                }))
                {
                    Trace?.Invoke(("What?", new byte[0]));
                }
            }
        }
    }
}