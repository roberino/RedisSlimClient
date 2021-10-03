using System;

namespace RedisTribute.WebStream
{
    public readonly struct RetrievalStats
    {
        public RetrievalStats(TimeSpan retrieveTime, TimeSpan parseTime, TimeSpan sendTime)
        {
            RetrieveTime = retrieveTime;
            ParseTime = parseTime;
            SendTime = sendTime;
        }

        public TimeSpan RetrieveTime { get; }
        public TimeSpan ParseTime { get; }
        public TimeSpan SendTime { get; }
    }
}
