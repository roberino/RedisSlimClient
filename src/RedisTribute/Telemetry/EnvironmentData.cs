using System.Threading;

namespace RedisTribute.Telemetry
{
    static class EnvironmentData
    {
        public static (int WorkerThreads, int IoThreads, int MinWorkerThreads, int MinIoThreads) GetThreadPoolUsage()
        {
            ThreadPool.GetMinThreads(out var wtMin, out var cptMin);
            ThreadPool.GetMaxThreads(out var wtMax, out var cptMax);
            ThreadPool.GetAvailableThreads(out var wt, out var cpt);

            return (wtMax - wt, cptMax - cpt, wtMin, cptMin);
        }
    }
}