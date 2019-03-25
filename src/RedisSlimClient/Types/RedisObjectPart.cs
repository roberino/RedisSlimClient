namespace RedisSlimClient.Types
{
    struct RedisObjectPart
    {
        public bool IsArrayStart { get; set; }
        public long Length { get; set; }
        public int Level { get; set; }
        public int? ArrayIndex { get; set; }
        public RedisObject Value { get; set; }
        public bool IsEmpty => Value == null;
        public bool IsArrayPart => ArrayIndex.HasValue;
    }
}