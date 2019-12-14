using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RedisTribute.Io.Server
{
    interface IServerNodeInitialiser
    {
        event Action ConfigurationChanged;
        Task<IReadOnlyCollection<IConnectionSubordinate>> CreateNodeSetAsync();
    }
}