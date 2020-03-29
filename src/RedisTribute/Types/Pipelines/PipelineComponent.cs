using System.Collections.Generic;

namespace RedisTribute.Types.Pipelines
{
    public abstract class PipelineBase<TRoot> where TRoot : IPipeline
    {
        internal PipelineBase(TRoot root = default)
        {
            Root = root;
        }

        internal TRoot Root { get; set; }
    }

    public abstract class PipelineComponent<TRoot, TData> : PipelineBase<TRoot> where TRoot : IPipeline
    {
        readonly List<IPipelineReceiver<TData>> _successors;

        internal PipelineComponent(TRoot root = default) : base(root)
        {
            _successors = new List<IPipelineReceiver<TData>>();
        }

        internal IReadOnlyCollection<IPipelineReceiver<TData>> Successors => _successors;

        internal void Attach(IPipelineReceiver<TData> successor)
        {
            if (successor is PipelineBase<TRoot> pc)
            {
                pc.Root = Root;
            }

            _successors.Add(successor);
        }
    }
}