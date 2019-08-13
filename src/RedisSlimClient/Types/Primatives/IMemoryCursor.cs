using System.Threading.Tasks;

namespace RedisSlimClient.Types.Primatives
{
    interface IMemoryCursor
    {
        int CurrentPosition { get; }

        Task Write(byte data);
        Task Write(byte[] data);
    }
}