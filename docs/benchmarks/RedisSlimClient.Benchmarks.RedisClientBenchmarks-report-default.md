
BenchmarkDotNet=v0.11.3, OS=Windows 10.0.17763.557 (1809/October2018Update/Redstone5)
Intel Core i7-8550U CPU 1.80GHz (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=2.2.105
  [Host] : .NET Core 2.2.3 (CoreCLR 4.6.27414.05, CoreFX 4.6.27414.05), 64bit RyuJIT
  Core   : .NET Core 2.2.3 (CoreCLR 4.6.27414.05, CoreFX 4.6.27414.05), 64bit RyuJIT

Job=Core  Runtime=Core  InvocationCount=1  
UnrollFactor=1  

         Method |  PipelineMode | ConnectionPoolSize | DataCollectionSize | ParallelOps |       Mean |     Error |     StdDev |     Median | Rank |
--------------- |-------------- |------------------- |------------------- |------------ |-----------:|----------:|-----------:|-----------:|-----:|
 **SetAndGetAsync** | **AsyncPipeline** |                  **1** |                  **5** |           **1** |   **916.4 us** |  **47.53 us** |   **137.1 us** |   **896.8 us** |    **1** |
 **SetAndGetAsync** | **AsyncPipeline** |                  **1** |                  **5** |           **4** |   **984.6 us** |  **50.02 us** |   **145.9 us** |   **967.1 us** |    **2** |
 **SetAndGetAsync** | **AsyncPipeline** |                  **1** |                 **10** |           **1** | **1,083.5 us** |  **53.72 us** |   **156.7 us** | **1,072.2 us** |    **2** |
 **SetAndGetAsync** | **AsyncPipeline** |                  **1** |                 **10** |           **4** | **1,176.1 us** |  **62.68 us** |   **180.8 us** | **1,172.5 us** |    **3** |
 **SetAndGetAsync** | **AsyncPipeline** |                  **4** |                  **5** |           **1** | **1,016.5 us** |  **58.01 us** |   **170.1 us** |   **977.6 us** |    **2** |
 **SetAndGetAsync** | **AsyncPipeline** |                  **4** |                  **5** |           **4** | **1,046.2 us** |  **57.26 us** |   **168.8 us** | **1,015.2 us** |    **2** |
 **SetAndGetAsync** | **AsyncPipeline** |                  **4** |                 **10** |           **1** | **1,121.2 us** |  **64.96 us** |   **190.5 us** | **1,078.7 us** |    **2** |
 **SetAndGetAsync** | **AsyncPipeline** |                  **4** |                 **10** |           **4** | **1,273.7 us** |  **56.15 us** |   **164.7 us** | **1,262.3 us** |    **4** |
 **SetAndGetAsync** |          **Sync** |                  **1** |                  **5** |           **1** | **1,649.4 us** |  **49.47 us** |   **143.5 us** | **1,640.5 us** |    **5** |
 **SetAndGetAsync** |          **Sync** |                  **1** |                  **5** |           **4** | **6,486.7 us** | **208.18 us** |   **569.9 us** | **6,500.7 us** |    **7** |
 **SetAndGetAsync** |          **Sync** |                  **1** |                 **10** |           **1** | **1,821.2 us** |  **53.89 us** |   **158.0 us** | **1,806.1 us** |    **6** |
 **SetAndGetAsync** |          **Sync** |                  **1** |                 **10** |           **4** | **7,349.0 us** | **523.56 us** | **1,476.7 us** | **6,903.8 us** |    **8** |
 **SetAndGetAsync** |          **Sync** |                  **4** |                  **5** |           **1** | **1,653.4 us** |  **54.36 us** |   **158.6 us** | **1,635.9 us** |    **5** |
 **SetAndGetAsync** |          **Sync** |                  **4** |                  **5** |           **4** | **6,428.7 us** | **263.23 us** |   **738.1 us** | **6,282.7 us** |    **7** |
 **SetAndGetAsync** |          **Sync** |                  **4** |                 **10** |           **1** | **1,851.1 us** |  **85.95 us** |   **241.0 us** | **1,780.0 us** |    **6** |
 **SetAndGetAsync** |          **Sync** |                  **4** |                 **10** |           **4** | **6,275.4 us** | **186.43 us** |   **522.8 us** | **6,216.9 us** |    **7** |