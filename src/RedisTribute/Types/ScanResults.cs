namespace RedisTribute.Types
{
    class ScanResults
    {
        public ScanResults(long cursorEnd, string[] keys)
        {
            Cursor = cursorEnd;
            Keys = keys;
        }

        public string[] Keys { get; }
        public long Cursor { get; }
    }
}
