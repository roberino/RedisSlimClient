using RedisSlimClient.Io.Commands;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisSlimClient.Io
{
    static class CancellationExtensions
    {
        public static async Task<T> ExecuteWithCancellation<T>(this ICommandExecutor pipe, IRedisResult<T> cmd, CancellationToken cancellation, TimeSpan defaultTimeout)
        {
            if (cancellation == default)
            {
                using (var cancel = new CancellationTokenSource(defaultTimeout))
                {
                    return await pipe.Execute(cmd, cancel.Token);
                }
            }

            return await pipe.Execute(cmd, cancellation);
        }

        public static async Task<T> ExecuteAdminWithTimeout<T>(this ICommandPipeline pipe, IRedisResult<T> cmd, TimeSpan defaultTimeout)
        {
            using (var cancel = new CancellationTokenSource(defaultTimeout))
            {
                return await pipe.ExecuteAdmin(cmd, cancel.Token);
            }
        }
    }
}