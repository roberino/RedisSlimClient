using System;
using RedisTribute.Serialization;

namespace RedisTribute.IntegrationTests.Data
{
    public static class ByteGeneration
    {
        static readonly Random _rnd = new Random();

        public static (string hash, byte[] data) RandomBytes()
        {
            var size = _rnd.Next(8096);
            var data = new byte[size];

            lock (_rnd)
            {
                _rnd.NextBytes(data);
            }

            return (data.CreateHash(), data);
        }
    }
}
