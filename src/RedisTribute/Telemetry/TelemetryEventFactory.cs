using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace RedisTribute.Telemetry
{
    class TelemetryEventFactory
    {

        readonly int _growRate;
        readonly ReaderWriterLockSlim _lock;
        readonly TelemetryEvent _default;
        readonly List<Rentable<TelemetryEvent>> _pool;
        readonly int _maxSize;

        public TelemetryEventFactory(int initialSize = 64, int maxSize = 1024)
        {
            _default = new TelemetryEvent();
            _lock = new ReaderWriterLockSlim();
            _pool = new List<Rentable<TelemetryEvent>>();
            _maxSize = maxSize;
            _growRate = Math.Max((int)(initialSize * (1 / 4f)), 1);

            ExpandPool(initialSize);
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
            _lock.EnterReadLock();

            try
            {
                foreach (var next in _pool.Where(x => x.Available))
                {
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
            finally
            {
                _lock.ExitReadLock();
            }

            ExpandPool(_growRate);

            return Create(name, operationId);
        }

        static Rentable<TelemetryEvent> SetupDispose(Rentable<TelemetryEvent> rentable)
        {
            rentable.Instance.OnReleased = rentable.Release;

            return rentable;
        }

        void ExpandPool(int numberOfItems)
        {
            _lock.EnterWriteLock();

            try
            {
                if (_pool.Count + numberOfItems > _maxSize)
                {
                    numberOfItems = _maxSize - _pool.Count;

                    if (numberOfItems <= 0)
                    {
                        throw new InvalidOperationException($"Pool size exceeded ({_pool.Count}/{_maxSize})");
                    }
                }

                _pool.AddRange(Enumerable.Range(1, numberOfItems).Select(x => SetupDispose(new Rentable<TelemetryEvent>(new TelemetryEvent()))));
            }
            finally
            {
                _lock.ExitWriteLock();
            }
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
