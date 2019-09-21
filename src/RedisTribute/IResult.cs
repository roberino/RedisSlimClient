using System;

namespace RedisTribute
{
    public interface IResult<T>
    {
        bool WasCancelled { get; }

        Result<T> IfCancelled(Action resultHandler);
        Result<T> IfFound(Action<T> resultHandler);
        Result<TTransform> IfFound<TTransform>(Func<T, TTransform> resultHandler);
        Result<TTransform> ResolveCancelled<TTransform>(Func<TTransform> resultHandler);
    }

    public interface IReadResult<T> : IResult<T>
    {
        bool WasFound { get; }
        Result<T> IfNotFound(Action resultHandler);
        Result<TTransform> ResolveNotFound<TTransform>(Func<TTransform> resultHandler);
    }
}