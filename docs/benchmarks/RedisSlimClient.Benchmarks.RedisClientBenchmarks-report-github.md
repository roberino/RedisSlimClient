``` ini

BenchmarkDotNet=v0.11.3, OS=Windows 10.0.17763.557 (1809/October2018Update/Redstone5)
Intel Core i7-8550U CPU 1.80GHz (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=2.2.105
  [Host] : .NET Core 2.2.3 (CoreCLR 4.6.27414.05, CoreFX 4.6.27414.05), 64bit RyuJIT
  Core   : .NET Core 2.2.3 (CoreCLR 4.6.27414.05, CoreFX 4.6.27414.05), 64bit RyuJIT

Job=Core  Runtime=Core  InvocationCount=1  
UnrollFactor=1  

```
|         Method |  PipelineMode | ConnectionPoolSize | DataCollectionSize | ParallelOps |       Mean |     Error |   StdDev | Rank |
|--------------- |-------------- |------------------- |------------------- |------------ |-----------:|----------:|---------:|-----:|
| **SetAndGetAsync** | **AsyncPipeline** |                  **1** |                  **5** |           **1** |   **969.6 us** |  **57.60 us** | **168.9 us** |    **1** |
| **SetAndGetAsync** | **AsyncPipeline** |                  **1** |                  **5** |           **4** |   **938.9 us** |  **42.55 us** | **122.1 us** |    **1** |
| **SetAndGetAsync** | **AsyncPipeline** |                  **1** |                 **10** |           **1** | **1,061.0 us** |  **58.22 us** | **167.0 us** |    **2** |
| **SetAndGetAsync** | **AsyncPipeline** |                  **1** |                 **10** |           **4** | **1,139.3 us** |  **55.66 us** | **161.5 us** |    **2** |
| **SetAndGetAsync** | **AsyncPipeline** |                  **4** |                  **5** |           **1** |   **901.1 us** |  **46.21 us** | **135.5 us** |    **1** |
| **SetAndGetAsync** | **AsyncPipeline** |                  **4** |                  **5** |           **4** |   **985.6 us** |  **35.05 us** | **101.7 us** |    **1** |
| **SetAndGetAsync** | **AsyncPipeline** |                  **4** |                 **10** |           **1** | **1,153.3 us** |  **69.92 us** | **206.2 us** |    **2** |
| **SetAndGetAsync** | **AsyncPipeline** |                  **4** |                 **10** |           **4** | **1,202.8 us** |  **39.49 us** | **114.6 us** |    **3** |
| **SetAndGetAsync** |          **Sync** |                  **1** |                  **5** |           **1** | **1,036.7 us** |  **53.31 us** | **156.3 us** |    **2** |
| **SetAndGetAsync** |          **Sync** |                  **1** |                  **5** |           **4** | **3,793.7 us** | **177.36 us** | **514.6 us** |    **4** |
| **SetAndGetAsync** |          **Sync** |                  **1** |                 **10** |           **1** | **1,238.9 us** |  **52.82 us** | **155.7 us** |    **3** |
| **SetAndGetAsync** |          **Sync** |                  **1** |                 **10** |           **4** | **4,789.8 us** | **236.52 us** | **686.2 us** |    **6** |
| **SetAndGetAsync** |          **Sync** |                  **4** |                  **5** |           **1** | **1,113.2 us** |  **58.50 us** | **171.6 us** |    **2** |
| **SetAndGetAsync** |          **Sync** |                  **4** |                  **5** |           **4** | **4,089.8 us** | **234.60 us** | **676.9 us** |    **5** |
| **SetAndGetAsync** |          **Sync** |                  **4** |                 **10** |           **1** | **1,227.5 us** |  **70.95 us** | **201.3 us** |    **3** |
| **SetAndGetAsync** |          **Sync** |                  **4** |                 **10** |           **4** | **4,868.2 us** | **341.17 us** | **984.4 us** |    **6** |
