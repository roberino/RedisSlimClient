using System;
using System.Collections.Generic;

namespace RedisTribute.Telemetry
{
    public class TelemetryEvent
    {
        readonly KeepAliveHandle _keepAliveHandle;

        Exception _exception;

        public TelemetryEvent()
        {
            _keepAliveHandle = new KeepAliveHandle(Release);
        }
                
        public static string CreateId() => Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public string Name { get; set; }

        public TelemetryCategory Category { get; set; } = TelemetryCategory.Internal;

        public Severity Severity { get; set; } = Severity.Info;

        public TelemetrySequence Sequence { get; set; } = TelemetrySequence.Start;

        public string OperationId { get; set; } = CreateId();

        public string Data { get; set; }

        public TimeSpan Elapsed { get; set; }

        public Exception Exception
        {
            get => _exception;
            set
            {
                _exception = value;
                Severity = Severity.Error;
            }
        }

        Action _onReleased;
        internal Action OnReleased
        {
            set
            {
                if (_onReleased != null)
                {
                    throw new InvalidOperationException();
                }
                _onReleased = value;
            }
        }

        internal IDisposable KeepAlive() => _keepAliveHandle;

        public IDictionary<string, object> Dimensions { get; } = new Dictionary<string, object>();

        void Release()
        {
            _onReleased?.Invoke();
        }

        class KeepAliveHandle : IDisposable
        {
            readonly Action _onDispose;

            public KeepAliveHandle(Action onDispose)
            {
                _onDispose = onDispose;
            }

            public void Dispose()
            {
                _onDispose.Invoke();
            }
        }
    }

    [Flags]
    public enum TelemetrySequence : byte
    {
        None = 0,
        Start = 1,
        Transitioning = 2,
        End = 4
    }

    [Flags]
    public enum TelemetryCategory : byte
    {
        None = 0,
        Internal = 1,
        Request = 2,
        Health = 4,
        Subscriber = 8
    }

    [Flags]
    public enum Severity : byte
    {
        None = 0,
        Info = 1,
        Warn = 2,
        Error = 4,
        Diagnostic = 8,
        All = Info | Warn | Error | Diagnostic
    }
}
