using System;
using System.Threading.Tasks;

namespace RedisTribute
{
    public static class ResultExtensions
    {
        public static async Task<Result<TTransform>> IfFound<T, TTransform>(this Task<Result<T>> resultTask, Func<T, Task<TTransform>> transform)
        {
            var result = await resultTask;

            var transformTask = result.IfFound(transform);

            if (transformTask.WasFound)
            {
                var value = await transformTask.AsValue();

                return Result<TTransform>.Found(value);
            }

            return Result<TTransform>.NotFound();
        }
    }
}
