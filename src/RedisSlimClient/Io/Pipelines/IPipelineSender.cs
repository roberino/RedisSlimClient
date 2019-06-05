using System.Threading.Tasks;

namespace RedisSlimClient.Io.Pipelines
{
    interface IPipelineSender
    {
        Task SendAsync(byte[] data);
    }
}
