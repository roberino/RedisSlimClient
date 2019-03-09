using System;
using System.Text;

namespace RedisSlimClient.Io
{
    static class DebugOutput
    {
        public static Action<string> Output { get; set; }

        public static void Dump(byte[] data, int length)
        {
#if DEBUG
            Output?.Invoke(Encoding.ASCII.GetString(data, 0, length));
#endif
        }
    }
}
