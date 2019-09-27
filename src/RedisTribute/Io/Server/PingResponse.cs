using RedisTribute.Io.Monitoring;
using System;

namespace RedisTribute.Io.Server
{
    public sealed class PingResponse
    {
        internal PingResponse(Uri serverEndpoint, bool ok, TimeSpan elapsed, PipelineMetrics metrics)
        {
            Endpoint = serverEndpoint;
            Ok = ok;
            Elapsed = elapsed;
            Metrics = metrics;
        }

        internal PingResponse(Uri serverEndpoint, Exception ex, TimeSpan elapsed, PipelineMetrics metrics)
        {
            Endpoint = serverEndpoint;
            Ok = false;
            Error = ex;
            Elapsed = elapsed;
            Metrics = metrics;
        }

        public Uri Endpoint { get; }
        public Exception Error { get; }
        public TimeSpan Elapsed { get; }
        public bool Ok { get; }
        internal PipelineMetrics Metrics { get; }
    }
}
