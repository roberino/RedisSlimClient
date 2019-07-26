using RedisSlimClient.Telemetry;
using System;

namespace RedisSlimClient.Io.Commands
{
    static class TelemetryExtensions
    {
        public static void AttachTelemetry(this IRedisCommand cmd, ITelemetryWriter writer)
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
                    var childEvent = new TelemetryEvent()
                    {
                        Name = s.Status.ToString(),
                        Elapsed = s.Elapsed,
                        OperationId = telemetryEvent.OperationId,
                        Data = cmd.AssignedEndpoint.ToString(),
                        Severity = s.Status == CommandStatus.Faulted ? Severity.Error : Severity.Info
                    };

                    childEvent.Dimensions[$"{nameof(Uri.Host)}"] = cmd.AssignedEndpoint.Host;
                    childEvent.Dimensions[$"{nameof(Uri.Port)}"] = cmd.AssignedEndpoint.Port;
                    childEvent.Dimensions["Role"] = cmd.AssignedEndpoint.Scheme;

                    writer.Write(childEvent);
                };
            }
        }
    }
}
