namespace RedisTribute.Configuration
{
    class KeySpace
    {
        public static readonly KeySpace Default = new KeySpace();

        public string GetLockKey(string key)
            => $"redlock://{key}";

        public string GetMessageLockKey(string key)
            => $"redlockm://{key}";

        public string GetCounterKey(string key)
            => $"counter://{key}";
        public string GetStreamKey(string key)
            => $"stream://{key}";
    }
}
