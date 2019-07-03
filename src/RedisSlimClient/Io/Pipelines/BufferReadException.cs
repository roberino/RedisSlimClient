using System;
using System.Buffers;
using System.Text;

namespace RedisSlimClient.Io.Pipelines
{
    public class BufferReadException : Exception
    {
        public BufferReadException(ReadOnlySequence<byte> data, Exception innerException) : base($"Buffer read error: {GetDumpText(data)}", innerException)
        {
        }

        static string GetDumpText(ReadOnlySequence<byte> data) => Encoding.UTF8.GetString(data.ToArray()).Replace("\r", "\\r").Replace("\n", "\\n");
    }
}