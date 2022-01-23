using System;
using System.Threading.Tasks;

namespace RedisTribute
{
    /// <summary>
    /// Options for scanning keys
    /// For match patterns see https://redis.io/commands/scan
    /// </summary>
    /// <remarks>
    /// SCAN cursor [MATCH pattern] [COUNT count] [TYPE type]
    /// </remarks>
    public readonly struct ScanOptions
    {
        public ScanOptions(Func<string, Task> resultsHandler, string? matchPattern = null)
        {
            ResultsHandler = resultsHandler;
            MatchPattern = matchPattern;
            MaxCount = null;
            BatchSize = null;
            Type = ScanType.Any;
        }

        public ScanOptions(Func<string, Task> resultsHandler, int batchSize, int maxCount, string matchPattern)
        {
            ResultsHandler = resultsHandler;
            MatchPattern = matchPattern;
            MaxCount = maxCount;
            BatchSize = batchSize;
            Type = ScanType.Any;
        }

        public Func<string, Task> ResultsHandler { get; }

        public string? MatchPattern { get; }

        public int? MaxCount { get; }

        public int? BatchSize { get; }

        public ScanType Type { get; }
    }

    public enum ScanType
    {
        Any,
        String,
        List,
        Set,
        ZSet,
        Hash,
        Stream
    }
}
