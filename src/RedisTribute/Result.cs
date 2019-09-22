using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute
{
    public readonly struct Result<T> : IReadResult<T>
    {
        readonly T _value;

        Result(T value, bool found, bool cancelled)
        {
            _value = value;
            WasFound = found;
            WasCancelled = cancelled;
        }

        public bool WasFound { get; }
        public bool WasCancelled { get; }

        internal static async Task<Result<T>> FromOperation(Func<Task<T>> op, CancellationToken cancellation)
        {
            if (cancellation.IsCancellationRequested)
            {
                return Cancelled();
            }

            try
            {
                var value = await op();

                return value == null ? NotFound() : Found(value);
            }
            catch (TaskCanceledException)
            {
                return Cancelled();
            }
            catch (AggregateException ex) when (ex.InnerExceptions.All(e => e is TaskCanceledException))
            {
                return Cancelled();
            }
            catch (Io.Commands.KeyNotFoundException)
            {
                return NotFound();
            }
        }

        public static Result<T> Found(T value)
        {
            return new Result<T>(value, true, false);
        }

        public static Result<T> NotFound()
        {
            return new Result<T>(default, false, false);
        }

        public static Result<T> Cancelled()
        {
            return new Result<T>(default, false, true);
        }

        public T AsValue()
        {
            if (WasCancelled)
            {
                throw new TaskCanceledException();
            }

            return _value;
        }

        public static implicit operator T(Result<T> result) => result.AsValue();

        public Result<TTransform> IfFound<TTransform>(Func<T, TTransform> resultHandler)
        {
            if (WasFound)
            {
                return Result<TTransform>.Found(resultHandler(_value));
            }

            if (WasCancelled)
            {
                return Result<TTransform>.Cancelled();
            }

            return Result<TTransform>.NotFound();
        }

        public Result<T> IfFound(Action<T> resultHandler)
        {
            if (WasFound)
            {
                resultHandler(_value);
            }

            return this;
        }

        public Result<TTransform> ResolveNotFound<TTransform>(Func<TTransform> resultHandler)
        {
            if (!WasFound && !WasCancelled)
            {
                return Result<TTransform>.Found(resultHandler());
            }

            if (WasFound && _value is TTransform tx)
            {
                return Result<TTransform>.Found(tx);
            }

            if (WasCancelled)
            {
                return Result<TTransform>.Cancelled();
            }

            return Result<TTransform>.NotFound();
        }

        public Result<T> IfNotFound(Action resultHandler)
        {
            if (!WasFound)
            {
                resultHandler();
            }

            return this;
        }

        public Result<TTransform> ResolveCancelled<TTransform>(Func<TTransform> resultHandler)
        {
            if (WasCancelled)
            {
                return Result<TTransform>.Found(resultHandler());
            }

            if (WasFound && _value is TTransform tx)
            {
                return Result<TTransform>.Found(tx);
            }

            return Result<TTransform>.NotFound();
        }

        public Result<T> IfCancelled(Action resultHandler)
        {
            if (WasCancelled)
            {
                resultHandler();
            }

            return this;
        }
    }
}