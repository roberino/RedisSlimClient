﻿using System;
using System.Collections.Generic;

namespace RedisSlimClient.Telemetry
{
    public class TelemetryEvent
    {
        public static TelemetryEvent CreateStart(string name) => new TelemetryEvent() { Name = name, Action = "Start" };
        public static TelemetryEvent CreateEnd(string name) => new TelemetryEvent() { Name = name, Action = "End" };

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public string Name { get; set; }

        public string Action { get; set; }

        public string OperationId { get; set; } = Guid.NewGuid().ToString("N").Substring(8);

        public string Data { get; set; }

        public TimeSpan Elapsed { get; set; }

        public Exception Exception { get; set; }

        public IDictionary<string, object> Dimensions { get; } = new Dictionary<string, object>();

        public TelemetryEvent CreateChild(string name) => new TelemetryEvent() { Name = name, OperationId = OperationId };
    }
}