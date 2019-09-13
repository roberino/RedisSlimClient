using System;
using System.Threading.Tasks;

namespace RedisTribute.Io.Pipelines
{
    interface IResetable
    {
        Task<IDisposable> ResetAsync();
    }
}
