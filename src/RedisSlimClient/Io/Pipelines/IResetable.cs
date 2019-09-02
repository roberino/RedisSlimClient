using System;
using System.Threading.Tasks;

namespace RedisSlimClient.Io.Pipelines
{
    interface IResetable
    {
        Task<IDisposable> ResetAsync();
    }
}
