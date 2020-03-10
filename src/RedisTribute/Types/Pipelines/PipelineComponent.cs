using System.Collections.Generic;

namespace RedisTribute.Types.Pipelines
{
    public abstract class PipelineComponent<TRoot, TData> where TRoot : IPipeline
    {
        readonly List<IPipelineReceiver<TData>> _successors;

        internal PipelineComponent(TRoot root = default)
        {
            _successors = new List<IPipelineReceiver<TData>>();
            Root=root;
        }

        internal TRoot Root { get; set; }

        internal IReadOnlyCollection<IPipelineReceiver<TData>> Successors => _successors;

        internal void Attach(IPipelineReceiver<TData> successor)
        {
            if (successor is PipelineComponent<TRoot, TData> pc)
            {
                pc.Root = Root;
            }

            _successors.Add(successor);
        }
    }
}
