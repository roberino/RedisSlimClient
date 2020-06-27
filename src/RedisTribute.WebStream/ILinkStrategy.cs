using System;
using System.Threading.Tasks;

namespace RedisTribute.WebStream
{
    public interface ILinkStrategy
    {
        bool ShouldVisit(Uri uri);

        Task<T> Visit<T>(Uri uri, Func<Task<T>> visiting, T defaultValue);
    }
}