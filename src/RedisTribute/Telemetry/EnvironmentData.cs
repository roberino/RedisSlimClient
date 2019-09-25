using System.Threading;

namespace RedisTribute.Telemetry
{
    static class EnvironmentData
    {
        public static (int WorkerThreads, int IoThreads) GetThreadPoolUsage()
        {
            ThreadPool.GetMinThreads(out var wtMin, out var cptMin);
            ThreadPool.GetMaxThreads(out var wtMax, out var cptMax);
            ThreadPool.GetAvailableThreads(out var wt, out var cpt);

            return (wtMin - (wtMax - wt), cptMin - (cptMax - cpt));
        }
    }
}