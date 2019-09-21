using System;
using System.Threading.Tasks;

namespace RedisTribute
{
    public static class ResultExtensions
    {
        public static async Task<Result<TTransform>> IfFound<T, TTransform>(this Task<T> resultTask, Func<T, Task<TTransform>> transform)
        {
            var result = await resultTask;

            return await resultTask.IfFound(transform);
        }
    }
}
