using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace RedisTribute.Telemetry
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

        public static async Task<T> ExecuteAsync<T>(this ITelemetryWriter writer, Func<TelemetricContext, Task<T>> act, string name = null, Severity severity = Severity.Info, TelemetryCategory category = TelemetryCategory.Internal)
        {
            if (name == null)
            {
                name = act.Method.Name;
            }

            var ev = TelemetryEventFactory.Instance.CreateStart(name);

            ev.Severity = severity;
            ev.Category = category;

            if (!writer.Enabled || !writer.Severity.HasFlag(severity) || !writer.Category.HasFlag(category))
            {
                using (ev.KeepAlive())
                    return await act(new TelemetricContext(NullWriter.Instance, ev, ev.Dimensions));
            }

            var endEv = TelemetryEventFactory.Instance.Create(name, ev.OperationId);

            endEv.Category = category;
            endEv.Sequence = TelemetrySequence.End;
            endEv.Severity = severity;

            var ctx = new TelemetricContext(writer, ev, endEv.Dimensions);

            var timer = new Stopwatch();

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