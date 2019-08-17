using System.Diagnostics;

namespace RedisSlimClient.Types
{
    [DebuggerDisplay("Len:{Length};Level:{Level};Index:{ArrayIndex};StartOfArray:{IsArrayStart}")]
    struct RedisObjectPart
    {
        public bool IsArrayStart { get; set; }
        public long Length { get; set; }
        public int Level { get; set; }
        public int? ArrayIndex { get; set; }
        public IRedisObject Value { get; set; }
        public bool IsEmpty => Value == null;
        public bool IsArrayPart => ArrayIndex.HasValue;
    }
}