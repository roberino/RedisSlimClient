using System.Linq;
using RedisTribute.Types;

namespace RedisTribute.Io.Commands.Keys
{
    class ScanCommand : RedisCommand<ScanResults>
    {
        readonly ScanOptions _options;
        readonly long _cursor;

        public ScanCommand(ScanOptions options, long cursor = 0) : base("SCAN", false, default)
        {
            _options = options;
            _cursor = cursor;
        }

        protected override CommandParameters GetArgs()
        {
            var args = new FixedSizeList<object>();

            args.IncreaseSize(2);
            args.IncreaseSizeIf(2, _options.MatchPattern != null);
            args.IncreaseSizeIf(2, _options.BatchSize.HasValue);
            args.IncreaseSizeIf(2, _options.Type != ScanType.Any);

            args.Add(CommandText);
            args.Add(_cursor.ToString());

            if (_options.MatchPattern != null)
            {
                args.Add("MATCH");
                args.Add(_options.MatchPattern);
            }

            if (_options.BatchSize.HasValue)
            {
                args.Add("COUNT");
                args.Add(_options.BatchSize.Value.ToString());
            }

            if (_options.Type != ScanType.Any)
            {
                args.Add("TYPE");
                args.Add(_options.Type.ToString().ToLower());
            }

            return args.GetBuffer();
        }

        protected override ScanResults TranslateResult(IRedisObject redisObject)
        {
            var arr = (RedisArray)redisObject;

            var cursor = arr[0].ToLong();
            var keys = (RedisArray)arr[1];

            return new ScanResults(cursor, keys.Select(k => k.ToString() ?? string.Empty).ToArray());
        }
    }
}
