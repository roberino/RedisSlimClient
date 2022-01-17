using System;

namespace RedisTribute.Types.Messaging
{
    public interface IMessageHeader
    {
        DateTime Timestamp { get; }

        string MachineName { get; }

        TimeSpan LockTime { get; }

        MessageFlags Flags { get; }
    }

    public class MessageHeader : IMessageHeader
    {
        public static MessageHeader Create(MessageFlags flags = MessageFlags.None, TimeSpan? lockTime = null)
        {
            return new MessageHeader
            {
                Timestamp = DateTime.UtcNow,
                MachineName = Environment.MachineName,
                Flags = flags,
                LockTime = lockTime.GetValueOrDefault(TimeSpan.FromMinutes(1))
            };
        }

        public string MachineName { get; set; } = string.Empty;
        public TimeSpan LockTime { get; set; }
        public MessageFlags Flags { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
