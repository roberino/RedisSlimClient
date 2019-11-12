using System;
using System.Collections.Generic;
using System.Text;

namespace RedisTribute.Io.Pipelines
{
    class PipelineRecoveryAgent
    {
        public PipelineRecoveryAgent(IDuplexPipeline pipeline)
        {
            pipeline.Receiver.Error += HandleError;
            pipeline.Sender.Error += HandleError;
            pipeline.Faulted += PipelineFaulted;
        }

        private void PipelineFaulted()
        {
            throw new NotImplementedException();
        }

        private void HandleError(Exception obj)
        {
            throw new NotImplementedException();
        }
    }
}
