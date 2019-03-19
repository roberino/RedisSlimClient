namespace RedisSlimClient.Types
{
    struct RedisObjectPart
    {
        public long Length { get; set; }
        public int? ArrayIndex { get; set; }
        public RedisObject Value { get; set; }
        public bool IsEmpty => Value == null;
        public bool IsArrayPart => ArrayIndex.HasValue;
    }
}