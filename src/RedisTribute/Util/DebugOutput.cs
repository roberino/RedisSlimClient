using System;
using System.Text;

namespace RedisTribute.Util
{
    static class DebugOutput
    {
        public static Action<string> Output { get; set; }

        public static void Dump(byte[] data, int length)
        {
#if DEBUG
            var msg = $"DUMP:{Encoding.ASCII.GetString(data, 0, length)}";
            Output?.Invoke(msg);
#endif
        }
    }
}
