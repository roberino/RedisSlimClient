using System;

namespace RedisTribute.Io
{
    class ObjectMovedException : Exception, IRedirectionInfo
    {
        const string MOVED = "MOVED";

        ObjectMovedException(int slot, Uri location) : base($"{MOVED} {slot} {location.Host}:{location.Port}")
        {
            Slot = slot;
            Location = location;
        }

        static ObjectMovedException Parse(string content)
        {
            // e.g.
            // MOVED 15101 127.0.0.1:7002

            var parts = content.Split(' ');

            var slot = int.Parse(parts[1]);
            var uri = new Uri($"redis://{parts[2]}");

            return new ObjectMovedException(slot, uri);
        }

        public static bool TryParse(string message, out ObjectMovedException exception)
        {
            if (message.StartsWith(MOVED, StringComparison.OrdinalIgnoreCase))
            {
                exception = Parse(message);

                return true;
            }

            exception = null;

            return false;
        }

        public int Slot { get; }

        public Uri Location { get; }
    }
}
