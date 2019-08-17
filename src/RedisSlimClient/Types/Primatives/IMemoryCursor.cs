using System.Threading.Tasks;

namespace RedisSlimClient.Types.Primatives
{
    interface IMemoryCursor
    {
        int CurrentPosition { get; }

        ValueTask<bool> Write(byte data);
        ValueTask<bool> Write(byte[] data);
    }
}