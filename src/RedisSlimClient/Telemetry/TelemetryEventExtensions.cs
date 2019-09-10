using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace RedisSlimClient.Telemetry
{
    static class TelemetryEventExtensions
    {
        public static void Execute(this ITelemetryWriter writer, Action<TelemetricContext> act, string name = null)
        {
            ExecuteAsync(writer, c =>
            {
                act(c);
                return Task.FromResult(true);
            }, name).GetAwaiter().GetResult();
        }

        public static async Task<T> ExecuteAsync<T>(this ITelemetryWriter writer, Func<TelemetricContext, Task<T>> act, string name = null, Severity severity = Severity.Info)
        {
            var timer = new Stopwatch();

            if (name == null)
            {
                name = act.Method.Name;
            }

            var ev = TelemetryEvent.CreateStart(name);

            ev.Severity = severity;

            var endEv = ev.CreateChild(name);

            endEv.Action = "End";
            endEv.Severity = severity;

            var ctx = new TelemetricContext(writer, ev, endEv.Dimensions);

            writer.Write(ev);

            timer.Start();

            T result;

            try
            {
                result = await act(ctx);

                endEv.Data = result?.ToString();
            }
            catch (Exception ex)
            {
                endEv.Exception = ex;
                throw;
            }
            finally
            {
                endEv.Elapsed = timer.Elapsed;
                writer.Write(endEv);
            }

            return result;
        }
    }
}