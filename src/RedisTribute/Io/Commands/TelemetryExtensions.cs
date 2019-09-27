using RedisTribute.Telemetry;
using System;
using System.Threading;

namespace RedisTribute.Io.Commands
{
    static class TelemetryExtensions
    {
        public static T AttachTelemetry<T>(this T cmd, ITelemetryWriter writer)
            where T : IRedisCommand
        {
            if (writer.Enabled && writer.Category.HasFlag(TelemetryCategory.Request))
            {
                var cmdName = cmd.GetType().Name;
                var cmdData = cmd.CommandText + (cmd.Key.IsNull ? string.Empty : $"/{cmd.Key}");

                var telemetryEvent = new TelemetryEvent()
                {
                    Name = cmdName,
                    Data = cmdData,
                    Sequence = TelemetrySequence.Start,
                    Category = TelemetryCategory.Request,
                    Severity = Severity.Info
                };

                writer.Write(telemetryEvent);

                cmd.OnStateChanged = s =>
                {
                    var level = (s.Status == CommandStatus.Faulted || s.Status == CommandStatus.Abandoned) ? Severity.Error : Severity.Info;

                    if (writer.Severity.HasFlag(level))
                    {
                        var end = s.Status == CommandStatus.Completed || s.Status == CommandStatus.Cancelled || s.Status == CommandStatus.Faulted || s.Status == CommandStatus.Abandoned;

                        var childEvent = new TelemetryEvent()
                        {
                            Name = $"{cmdName}/{s.Status}",
                            Data = cmdData,
                            Category = TelemetryCategory.Request,
                            Sequence = end ? TelemetrySequence.End : TelemetrySequence.Transitioning,
                            Elapsed = s.Elapsed,
                            OperationId = telemetryEvent.OperationId,
                            Severity = level
                        };

                        if (level == Severity.Error)
                        {
                            var threads = EnvironmentData.GetThreadPoolUsage();

                            childEvent.Dimensions["WT"] = threads.WorkerThreads;
                            childEvent.Dimensions["CPT"] = threads.IoThreads;
                        }

                        childEvent.Dimensions[$"{nameof(Uri.Host)}"] = cmd.AssignedEndpoint.Host;
                        childEvent.Dimensions[$"{nameof(Uri.Port)}"] = cmd.AssignedEndpoint.Port;
                        childEvent.Dimensions["Role"] = cmd.AssignedEndpoint.Scheme;

                        writer.Write(childEvent);
                    }
                };
            }

            return cmd;
        }
    }
}
