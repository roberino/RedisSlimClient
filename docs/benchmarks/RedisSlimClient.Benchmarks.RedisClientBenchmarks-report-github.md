``` ini

BenchmarkDotNet=v0.11.3, OS=Windows 10.0.17763.557 (1809/October2018Update/Redstone5)
Intel Core i7-8550U CPU 1.80GHz (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=2.2.105
  [Host] : .NET Core 2.2.3 (CoreCLR 4.6.27414.05, CoreFX 4.6.27414.05), 64bit RyuJIT
  Core   : .NET Core 2.2.3 (CoreCLR 4.6.27414.05, CoreFX 4.6.27414.05), 64bit RyuJIT

Job=Core  Runtime=Core  InvocationCount=1  
UnrollFactor=1  

```
|         Method |  PipelineMode | ConnectionPoolSize | DataCollectionSize | ParallelOps |      Mean |     Error |    StdDev |    Median | Rank |
|--------------- |-------------- |------------------- |------------------- |------------ |----------:|----------:|----------:|----------:|-----:|
| **SetAndGetAsync** | **AsyncPipeline** |                  **1** |                  **5** |           **1** |  **3.390 ms** | **0.1759 ms** | **0.5186 ms** |  **3.289 ms** |    **1** |
| **SetAndGetAsync** | **AsyncPipeline** |                  **1** |                  **5** |           **4** |  **6.253 ms** | **0.3885 ms** | **1.1456 ms** |  **6.340 ms** |    **5** |
| **SetAndGetAsync** | **AsyncPipeline** |                  **1** |                 **10** |           **1** |  **3.206 ms** | **0.1472 ms** | **0.4224 ms** |  **3.160 ms** |    **1** |
| **SetAndGetAsync** | **AsyncPipeline** |                  **1** |                 **10** |           **4** |  **7.143 ms** | **0.3399 ms** | **1.0022 ms** |  **7.004 ms** |    **6** |
| **SetAndGetAsync** | **AsyncPipeline** |                  **4** |                  **5** |           **1** |  **4.202 ms** | **0.2520 ms** | **0.7392 ms** |  **4.112 ms** |    **3** |
| **SetAndGetAsync** | **AsyncPipeline** |                  **4** |                  **5** |           **4** |  **8.396 ms** | **0.4107 ms** | **1.1784 ms** |  **8.451 ms** |    **8** |
| **SetAndGetAsync** | **AsyncPipeline** |                  **4** |                 **10** |           **1** |  **3.253 ms** | **0.1651 ms** | **0.4603 ms** |  **3.222 ms** |    **1** |
| **SetAndGetAsync** | **AsyncPipeline** |                  **4** |                 **10** |           **4** |  **8.118 ms** | **0.3157 ms** | **0.9260 ms** |  **8.039 ms** |    **7** |
| **SetAndGetAsync** |          **Sync** |                  **1** |                  **5** |           **1** |  **4.766 ms** | **0.3612 ms** | **1.0593 ms** |  **4.496 ms** |    **4** |
| **SetAndGetAsync** |          **Sync** |                  **1** |                  **5** |           **4** | **16.264 ms** | **0.9552 ms** | **2.5496 ms** | **15.494 ms** |    **9** |
| **SetAndGetAsync** |          **Sync** |                  **1** |                 **10** |           **1** |  **4.848 ms** | **0.3694 ms** | **1.0833 ms** |  **4.575 ms** |    **4** |
| **SetAndGetAsync** |          **Sync** |                  **1** |                 **10** |           **4** | **18.770 ms** | **1.2845 ms** | **3.7874 ms** | **18.697 ms** |   **10** |
| **SetAndGetAsync** |          **Sync** |                  **4** |                  **5** |           **1** |  **3.951 ms** | **0.1576 ms** | **0.4520 ms** |  **3.864 ms** |    **2** |
| **SetAndGetAsync** |          **Sync** |                  **4** |                  **5** |           **4** | **18.084 ms** | **1.2282 ms** | **3.6020 ms** | **17.811 ms** |   **10** |
| **SetAndGetAsync** |          **Sync** |                  **4** |                 **10** |           **1** |  **4.517 ms** | **0.2896 ms** | **0.8357 ms** |  **4.390 ms** |    **4** |
| **SetAndGetAsync** |          **Sync** |                  **4** |                 **10** |           **4** | **17.852 ms** | **1.1610 ms** | **3.3498 ms** | **17.170 ms** |   **10** |
