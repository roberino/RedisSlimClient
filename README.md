[![Build status](https://ci.appveyor.com/api/projects/status/0eagkgc04t1jvg1m?svg=true)](https://ci.appveyor.com/project/roberino/RedisSlimClient)

[![Build Status](https://travis-ci.org/roberino/RedisSlimClient.svg?branch=master)](https://travis-ci.org/roberino/RedisSlimClient)

# RedisTribute

RedisTribute is a dotnet standard client for Redis, written from the ground up.

[Download NuGet package](https://www.nuget.org/packages/RedisTribute/)

![logo](docs/logo.png "RedisTribute")

The main aims of the client are:

* To create a pluggable, fault tolerant Redis client for .NET (with a focus on supporting DotNet Core / Standard)
* A simple, async interface with support for cancellation tokens
* To implement basic Redis operations
* To enable fast POCO to Redis mapping
* To be performant with granular control over thread and socket usage
* Expose detailed telemetry to enable diagnostic analysis and monitoring

# Basic usage

```cs

// NOTE: The client is designed to be used as a singleton and can be shared across threads - there is an overhead in creating new clients each call

using (var client = ((ClientConfiguration)"localhost:6379").CreateClient())
{
    await client.SetAsync("key1", "Hello world!");

    var result = await client.GetAsync<string>("key1");

    result.IfFound(Console.WriteLine);
}

```

## Additional topics

* [Distributed Locks](docs/DistributedLocks.md)
* [HashSets](docs/HashSets.md)
* [Counters](docs/Counters.md)
* [Graphs](docs/Graphs.md)
* [Simple Pub/sub support](docs/PubSub.md)
* [Geo API](docs/GeoApi.md)

# Configuration

## Minimum configuration

The minimal configuration must include a host and/or port name. Each additional setting must be separated by semi-colon. 
Azure configuration strings (from the portal) are also supported.

e.g.

```

localhost:6379;Password=p@ssw0rd

```

## Additional settings


| Setting                 | Usage                                                                                                                                             | Format                         | Notes                                                           |
|-------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------|--------------------------------|-----------------------------------------------------------------|
| Password                | Sets the password                                                                                                                                 | String                         | Optional (required with requirepass option is used)             |
| ClientName              | Sets the name of the client                                                                                                                       | String (no spaces)             | Optional (will default to a combination of machine and process) |
| UseSsl                  | When true, communication will be performed over a SSL connection                                                                                  | Bool                           | Optional (required if an SSL channel is required)               |
| SslHost                 | Sets the host name used for the SSL connection                                                                                                    | String                         | Optional (will default to the Redis server host name)           |
| CertificatePath         | Sets the path to the SSL certificate used to communicate with Redis                                                                               | String                         | Optional (required if SSL enabled)                              |
| DefaultOperationTimeout | Sets the default timeout for Redis commands (see timeout behaviour below)                                                                         | Timespan                       | Optional                                                        |
| ConnectTimeout          | Sets the timeout for connecting to a single Redis node (includes auth and initialising commands)                                                  | Timespan                       | Optional                                                        |
| HealthCheckInterval	  | Sets the interval between health checks from the client to the server                                                                             | Timespan                       | Optional                                                        |
| ConnectionPoolSize      | Sets a number which creates a pool of available connections                                                                                       | Integer (> 0)                  | Optional (will default to 1)                                    |
| ReadBufferSize          | Sets the size of the network read buffer                                                                                                          | Integer                        | Optional                                                        |
| WriteBufferSize         | Sets the size of the network write buffer                                                                                                         | Integer                        | Optional                                                        |
| PipelineMode            | Sets the mode of retrieval from the TCP connection (AsyncPipeline will pipeline multiple commands, Sync will send one command at a time)          | AsyncPipeline|Sync             | Optional (defaults to AsyncPipeline)                            |
| PortMappings            | Used to apply a set of port mappings from an external port to an internal Redis port (e.g. when using a container environment with port mappings) | CSV (e.g. from1:to1,from2:to2) | Optional                                                        |
| FallbackStrategy        | Determines the retry behaviour of the client                                                                                                      | None|Retry|ProactiveRetry      | Optional (defaults to Retry)                                    |

### Timeouts and retry behaviour

Each client method supports the use of cancellation tokens. If provided, the cancellation token will determine when an operation is cancelled. If not provided, then the DefaultOperationTimeout value will be used to determing the timeout.

The DefaultOperationTimeout should be set reasonably high to cater for the cost of reconnecting during operations.

If Retry is enabled, the client will retry the request on another connection if possible until cancellation is requested. 
ProactiveRetry will begin to retry the operation on another connection if the previous request takes too long, returning the first available response.

# Main Features

* Support for SSL
* Flexible thread management
* Support for clusters and replica configurations
* Support for master/slave configurations
* Keep alive & socket monitoring
* Retry logic
* Azure compatibility
* Retry handling
* Telemetry
* Scan / Get / MGet / Set
* Support for GEO commands
* [Distributed Locks](docs/DistributedLocks.md)
* [HashSets](docs/HashSets.md)
* [Counters](docs/Counters.md)
* [Graphs](docs/Graphs.md)
* [Simple Pub/sub support](docs/PubSub.md)

# Extension packages

## RedisTribute.Json

This package adds a JSON serializer to the configuration for object to Redis mapping.

```cs

var config = new ClientConfiguration("localhost:6379").UseJsonSerialization();

```

## RedisTribute.ApplicationInsights

This package adds Application Insights integration into the client so that calls to Redis are tracked as dependencies.

[See application insights docs here](docs/ApplicationInsights.md)

# Benchmarks

[See benchmarks here](docs/benchmarks/RedisTribute.Benchmarks.RedisClientBenchmarks-report-github.md)

# TODO

* Binary keys
* Expose info
* Transactions
* Support for clustering redirection (requires more testing)
* Better memory management
* Slimmer object serialization