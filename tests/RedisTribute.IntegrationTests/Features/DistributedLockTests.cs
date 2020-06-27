using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using RedisTribute.Configuration;
using Xunit;
using Xunit.Abstractions;

namespace RedisTribute.IntegrationTests.Features
{
    public class DistributedLockTests
    {
        readonly ITestOutputHelper _output;

        public DistributedLockTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData(PipelineMode.Sync, ConfigurationScenario.NonSslBasic)]
        public async Task AquireLockAsync_InnerLockInSyncPipelineMode_CanBeAquired(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine);

            using (var client = config.CreateClient())
            {
                await client.PingAllAsync();

                var lockKey = Guid.NewGuid().ToString();

                using (var asyncLock = await client.AcquireLockAsync(lockKey, new LockOptions(TimeSpan.FromSeconds(5), true)))
                {
                    Assert.False(asyncLock.LockExpired);

                    using (var subLock = await client.AcquireLockAsync(lockKey, LockOptions.AllowRecursiveLocks))
                    {
                        Assert.Equal(subLock.Key, asyncLock.Key);
                    }

                    await asyncLock.ReleaseLockAsync();
                }
            }
        }

        [Theory]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslBasic)]
        public async Task AquireLockAsync_InnerLockInWithSyncWaits_CanBeAquired(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine);

            using (var client = config.CreateClient())
            {
                await client.PingAllAsync();

                var lockKey = Guid.NewGuid().ToString();

                using (var asyncLock = client.AcquireLockAsync(lockKey, new LockOptions(TimeSpan.FromSeconds(5), true)).GetAwaiter().GetResult())
                {
                    Assert.False(asyncLock.LockExpired);

                    using (var subLock = await client.AcquireLockAsync(lockKey, LockOptions.AllowRecursiveLocks))
                    {
                        Assert.Equal(subLock.Key, asyncLock.Key);
                    }

                    await asyncLock.ReleaseLockAsync();
                }
            }
        }

        [Theory]
        [InlineData(PipelineMode.Sync, ConfigurationScenario.NonSslBasic)]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslBasic)]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslReplicaSetMaster)]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslClusterSet)]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.SslBasic)]
        public async Task AquireLockAsync_AquireLockOnOtherThread(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine);
            var locksAquired = new ConcurrentBag<IDistributedLock>();

            using (var client = config.CreateClient())
            {
                await client.PingAllAsync();

                var lockKey = Guid.NewGuid().ToString();

                Enumerable.Range(1, 3).AsParallel().ForAll(n =>
                {
                    _output.WriteLine($"Aquiring lock for {n}");

                    try
                    {
                        var asyncLock = client.AcquireLockAsync(lockKey, new LockOptions(TimeSpan.FromSeconds(15), false)).GetAwaiter().GetResult();

                        _output.WriteLine($"Aquired lock for {n} for {asyncLock.RemainingTime}");

                        locksAquired.Add(asyncLock);
                    }
                    catch (Exception ex)
                    {
                        _output.WriteLine($"Aquire lock failed for {n} : {ex.Message}");
                    }
                });

                foreach (var aquiredLock in locksAquired)
                {
                    aquiredLock.Dispose();
                }

                Assert.Single(locksAquired);
            }
        }
    }
}