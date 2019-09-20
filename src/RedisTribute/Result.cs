using System;
using System.Threading.Tasks;

namespace RedisTribute
{
    public readonly struct Result<T>
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

        public static implicit operator T(Result<T> result) => result._value;

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

        public Result<TTransform> IfNotFound<TTransform>(Func<TTransform> resultHandler)
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

        public Result<TTransform> IfCancelled<TTransform>(Func<TTransform> resultHandler)
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

        //public async Task IfFound(Func<T, Task> resultHandler)
        //{
        //    if (WasFound)
        //    {
        //        await resultHandler(_value);
        //    }
        //}

        //public async Task IfNotFound(Func<Task> resultHandler)
        //{
        //    if (!WasFound)
        //    {
        //        await resultHandler();
        //    }
        //}

        //public async Task IfCancelled(Func<Task> resultHandler)
        //{
        //    if (!WasCancelled)
        //    {
        //        await resultHandler();
        //    }
        //}
    }
}