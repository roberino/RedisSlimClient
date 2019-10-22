using System;

namespace RedisTribute
{
    public readonly struct Expiry
    {
        readonly bool _millisecGranularity;

        public Expiry(TimeSpan? timespan)
        {
            if (timespan.HasValue)
            {
                _millisecGranularity = timespan.Value.Milliseconds > 0;
                Value = timespan.Value;
                HasValue = true;
            }
            else
            {
                _millisecGranularity = false;
                Value = TimeSpan.MinValue;
                HasValue = false;
            }
        }

        public static Expiry Infinite => new Expiry(null);

        public bool HasValue { get; }

        public TimeSpan Value { get; }

        public int IntValue => (int)(_millisecGranularity ? Value.TotalMilliseconds : Value.TotalSeconds);

        public string Type => _millisecGranularity ? "PX" : "EX";

        public static implicit operator Expiry(TimeSpan x) => new Expiry(x);

        public static implicit operator Expiry(DateTime x) => new Expiry(x - DateTime.UtcNow);
    }
}
