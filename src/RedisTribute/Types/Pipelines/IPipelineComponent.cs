using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.Types.Pipelines
{
    public interface IPipeline
    {
        Task ExecuteAsync(CancellationToken cancellation = default);
    }

    interface IPipelineComponent<in TIn, TOut> : IPipelineReceiver<TIn>, IHasSuccessor<TOut>
    {
    }

    interface IHasSuccessor<T>
    {
    }

    interface IPipelineReceiver<in TIn>
    {
        Task ReceiveAsync(TIn input, CancellationToken cancellation);
    }
}
