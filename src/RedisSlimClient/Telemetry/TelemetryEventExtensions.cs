using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace RedisSlimClient.Telemetry
{
    static class TelemetryEventExtensions
    {
        public static async Task<T> ExecuteAsync<T>(this ITelemetryWriter writer, Func<TelemetricContext, Task<T>> act, string name)
        {
            var timer = new Stopwatch();

            var ev = TelemetryEvent.CreateStart(name);
            var ctx = new TelemetricContext(writer, ev);
            var endEv = ev.CreateChild(name);
            endEv.Action = "End";

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