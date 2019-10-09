using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace RedisTribute.Telemetry
{
    class TelemetryEventFactory
    {
        readonly TelemetryEvent _default;
        readonly List<Rentable<TelemetryEvent>> _pool;
        private readonly int _maxSize;

        public TelemetryEventFactory(int initialSize = 64, int maxSize = 1024)
        {
            _default = new TelemetryEvent();
            _pool = Enumerable.Range(1, initialSize).Select(x => SetupDispose(new Rentable<TelemetryEvent>(new TelemetryEvent()))).ToList();
            _maxSize = maxSize;
        }

        public static TelemetryEventFactory Instance { get; } = new TelemetryEventFactory();

        public int Size => _pool.Count;

        public int Available => _pool.Count(x => x.Available);

        public TelemetryEvent CreateStart(string name)
        {
            var ev = Create(name);
            ev.Sequence = TelemetrySequence.Start;
            return ev;
        }

        public TelemetryEvent Create(string name, string operationId = null)
        {
            while (true)
            {
                var next = _pool.FirstOrDefault(x => x.Available);

                if (next == null)
                {
                    if (_pool.Count >= _maxSize)
                    {
                        throw new InvalidOperationException($"Pool size exceeded ({_pool.Count}/{_maxSize})");
                    }

                    next = SetupDispose(new Rentable<TelemetryEvent>(new TelemetryEvent()
                    {
                        Name = name,
                        OperationId = operationId ?? TelemetryEvent.CreateId()
                    }));

                    lock(_pool)
                        _pool.Add(next);

                    if (next.Rent())
                    {
                        return next.Instance;
                    }
                }

                if (next.Rent())
                {
                    next.Instance.Name = name;
                    next.Instance.Category = _default.Category;
                    next.Instance.Elapsed = _default.Elapsed;
                    next.Instance.Exception = _default.Exception;
                    next.Instance.Severity = _default.Severity;
                    next.Instance.Sequence = _default.Sequence;
                    next.Instance.Timestamp = DateTime.UtcNow;
                    next.Instance.Dimensions.Clear();
                    next.Instance.OperationId = operationId ?? TelemetryEvent.CreateId();

                    return next.Instance;
                }
            }
        }

        static Rentable<TelemetryEvent> SetupDispose(Rentable<TelemetryEvent> rentable)
        {
            rentable.Instance.OnDispose = () => rentable.Release();

            return rentable;
        }

        class Rentable<T>
        {
            readonly object _lock = new object();
            int _rented;

            public Rentable(T instance)
            {
                Instance = instance;
            }

            public T Instance { get; }

            public bool Available => _rented == 0;

            public bool Rent()
            {
                return Interlocked.CompareExchange(ref _rented, 1, 0) == 0;
            }

            public void Release()
            {
                Interlocked.CompareExchange(ref _rented, 0, 1);
            }
        }
    }
}
