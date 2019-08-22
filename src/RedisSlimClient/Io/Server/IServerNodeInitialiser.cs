using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RedisSlimClient.Io.Server
{
    interface IServerNodeInitialiser
    {
        event Action ConfigurationChanged;
        Task<IReadOnlyCollection<IConnectionSubordinate>> InitialiseAsync();
    }
}