using RedisSlimClient.Io.Commands;
using RedisSlimClient.Io.Scheduling;
using RedisSlimClient.Serialization;
using RedisSlimClient.Telemetry;
using RedisSlimClient.Types;
using RedisSlimClient.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace RedisSlimClient.Io
{
    internal class AsyncCommandPipeline : ICommandPipeline
    {
        readonly SyncedCounter _pendingWrites = new SyncedCounter();

        readonly Stream _writeStream;
        readonly ITelemetryWriter _telemetryWriter;
        readonly IEnumerable<RedisObjectPart> _reader;
        readonly CommandQueue _commandQueue;
        readonly IWorkScheduler _scheduler;

        bool _disposed;

        public AsyncCommandPipeline(Stream networkStream, ITelemetryWriter telemetryWriter)
        {
            _writeStream = networkStream;
            _telemetryWriter = telemetryWriter ?? NullWriter.Instance;
            _reader = new ArraySegmentToRedisObjectReader(new StreamIterator(networkStream));
            _commandQueue = new CommandQueue();
        }

        public (int PendingWrites, int PendingReads) PendingWork => ((int)_pendingWrites.Value, _commandQueue.QueueSize);

        public Task<T> Execute<T>(IRedisResult<T> command, TimeSpan timeout)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(CommandPipeline));
            }

            throw new NotImplementedException();
        }

        public void Dispose()
        {
            _disposed = true;
            _scheduler.Dispose();
        }
    }
}