# Application Insights

## Basic Usage

```cs

config.UseApplicationInsights("<instrumentation-key-here>"); // or config or telemetry client instance

```

## Example Queries

```
// 75th percentile of request duration over time

dependencies
| project timestamp, duration, target, success 
| summarize percentile(duration, 75) by bin(timestamp, 5s), success
| render timechart   

// Requests per second (RPS) over time

dependencies
| project timestamp, duration, target, success, itemCount 
| summarize sum(itemCount) by bin(timestamp, 1s), success
| render timechart   

// Pooled memory usage over time

availabilityResults
| project timestamp, duration, success, PooledMemory = toint(customDimensions.PooledMemory) 
| summarize max(PooledMemory) by bin(timestamp, 1s), success
| render timechart   

// Thread pool usage over time

availabilityResults
| project timestamp, duration, success, WT = toint(customDimensions.WT), CPT = toint(customDimensions.WT), CPTMin=toint(customDimensions.MinCPT), WTMin=toint(customDimensions.MinWT) 
| summarize max(WT), max(CPT), min(CPTMin), min(WTMin) by bin(timestamp, 1s), success
| render timechart 

```