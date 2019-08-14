using RedisSlimClient.Telemetry;
using System;

namespace RedisSlimClient.Io.Commands
{
    static class TelemetryExtensions
    {
        public static T AttachTelemetry<T>(this T cmd, ITelemetryWriter writer)
            where T : IRedisCommand
        {
            if (writer.Enabled)
            {
                var telemetryEvent = new TelemetryEvent()
                {
                    Name = nameof(cmd.Execute),
                    Action = cmd.CommandText
                };

                writer.Write(telemetryEvent);

                cmd.OnStateChanged = s =>
                {
                    var level = s.Status == CommandStatus.Faulted ? Severity.Error : Severity.Info;

                    if (writer.Severity.HasFlag(level))
                    {
                        var childEvent = new TelemetryEvent()
                        {
                            Name = s.Status.ToString(),
                            Elapsed = s.Elapsed,
                            OperationId = telemetryEvent.OperationId,
                            Data = cmd.AssignedEndpoint.ToString(),
                            Severity = level
                        };

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
