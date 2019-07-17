using System.Collections.Generic;
using System.Threading.Tasks;

namespace RedisSlimClient.Io.Server
{
    interface IServerNodeInitialiser
    {
        Task<IReadOnlyCollection<IConnectionSubordinate>> InitialiseAsync();
    }
}