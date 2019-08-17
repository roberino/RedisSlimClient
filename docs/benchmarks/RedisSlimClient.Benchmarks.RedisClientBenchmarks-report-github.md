``` ini

BenchmarkDotNet=v0.11.3, OS=Windows 10.0.17763.557 (1809/October2018Update/Redstone5)
Intel Core i7-8550U CPU 1.80GHz (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=2.2.105
  [Host] : .NET Core 2.2.3 (CoreCLR 4.6.27414.05, CoreFX 4.6.27414.05), 64bit RyuJIT
  Core   : .NET Core 2.2.3 (CoreCLR 4.6.27414.05, CoreFX 4.6.27414.05), 64bit RyuJIT

Job=Core  Runtime=Core  InvocationCount=1  
UnrollFactor=1  

```
|         Method |  PipelineMode | ConnectionPoolSize | DataCollectionSize | ParallelOps |     Mean |     Error |    StdDev |   Median | Rank |
|--------------- |-------------- |------------------- |------------------- |------------ |---------:|----------:|----------:|---------:|-----:|
| **SetAndGetAsync** | **AsyncPipeline** |                  **1** |                  **5** |           **1** | **1.314 ms** | **0.0464 ms** | **0.1248 ms** | **1.296 ms** |    **2** |
| **SetAndGetAsync** | **AsyncPipeline** |                  **1** |                  **5** |           **4** | **1.184 ms** | **0.0553 ms** | **0.1586 ms** | **1.184 ms** |    **1** |
| **SetAndGetAsync** | **AsyncPipeline** |                  **1** |                 **10** |           **1** | **1.466 ms** | **0.0435 ms** | **0.1220 ms** | **1.447 ms** |    **4** |
| **SetAndGetAsync** | **AsyncPipeline** |                  **1** |                 **10** |           **4** | **1.576 ms** | **0.0610 ms** | **0.1789 ms** | **1.561 ms** |    **4** |
| **SetAndGetAsync** | **AsyncPipeline** |                  **4** |                  **5** |           **1** | **1.532 ms** | **0.1160 ms** | **0.3422 ms** | **1.442 ms** |    **4** |
| **SetAndGetAsync** | **AsyncPipeline** |                  **4** |                  **5** |           **4** | **1.305 ms** | **0.0657 ms** | **0.1918 ms** | **1.296 ms** |    **2** |
| **SetAndGetAsync** | **AsyncPipeline** |                  **4** |                 **10** |           **1** | **1.619 ms** | **0.1053 ms** | **0.3089 ms** | **1.518 ms** |    **4** |
| **SetAndGetAsync** | **AsyncPipeline** |                  **4** |                 **10** |           **4** | **1.742 ms** | **0.0753 ms** | **0.2219 ms** | **1.742 ms** |    **5** |
| **SetAndGetAsync** |          **Sync** |                  **1** |                  **5** |           **1** | **1.238 ms** | **0.0643 ms** | **0.1833 ms** | **1.188 ms** |    **1** |
| **SetAndGetAsync** |          **Sync** |                  **1** |                  **5** |           **4** | **4.601 ms** | **0.2374 ms** | **0.7001 ms** | **4.512 ms** |    **6** |
| **SetAndGetAsync** |          **Sync** |                  **1** |                 **10** |           **1** | **1.411 ms** | **0.0726 ms** | **0.2060 ms** | **1.375 ms** |    **3** |
| **SetAndGetAsync** |          **Sync** |                  **1** |                 **10** |           **4** | **5.562 ms** | **0.2677 ms** | **0.7768 ms** | **5.451 ms** |    **8** |
| **SetAndGetAsync** |          **Sync** |                  **4** |                  **5** |           **1** | **1.333 ms** | **0.0728 ms** | **0.2111 ms** | **1.303 ms** |    **2** |
| **SetAndGetAsync** |          **Sync** |                  **4** |                  **5** |           **4** | **4.980 ms** | **0.2962 ms** | **0.8641 ms** | **5.033 ms** |    **7** |
| **SetAndGetAsync** |          **Sync** |                  **4** |                 **10** |           **1** | **1.481 ms** | **0.0590 ms** | **0.1693 ms** | **1.426 ms** |    **4** |
| **SetAndGetAsync** |          **Sync** |                  **4** |                 **10** |           **4** | **5.548 ms** | **0.3040 ms** | **0.8524 ms** | **5.370 ms** |    **8** |
