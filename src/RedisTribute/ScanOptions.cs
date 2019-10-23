using System;
using System.Threading.Tasks;

namespace RedisTribute
{
    /// <summary>
    /// Options for scanning keys
    /// </summary>
    /// <remarks>
    /// SCAN cursor [MATCH pattern] [COUNT count] [TYPE type]
    /// </remarks>
    public readonly struct ScanOptions
    {
        public ScanOptions(Func<string, Task> resultsHandler, string matchPattern = null, ScanType type = ScanType.Any)
        {
            ResultsHandler = resultsHandler;
            MatchPattern = matchPattern;
            MaxCount = null;
            BatchSize = null;
            Type = type;
        }

        public ScanOptions(Func<string, Task> resultsHandler, int batchSize, int maxCount, ScanType type, string matchPattern)
        {
            ResultsHandler = resultsHandler;
            MatchPattern = matchPattern;
            MaxCount = maxCount;
            BatchSize = batchSize;
            Type = type;
        }

        public Func<string, Task> ResultsHandler { get; }

        public string MatchPattern { get; }

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
