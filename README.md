[![Build status](https://ci.appveyor.com/api/projects/status/0eagkgc04t1jvg1m?svg=true)](https://ci.appveyor.com/project/roberino/redisslimclient)

[![Build Status](https://travis-ci.org/roberino/RedisSlimClient.svg?branch=master)](https://travis-ci.org/roberino/RedisSlimClient)

# RedisSlimClient

A work in progress. RedisSlimClient is a dotnet standard client for Redis, written from the ground up.

The main aims of the client are:

* To support basic Redis operations
* To enable fast POCO to Redis mapping
* To be performant with granular control over thread and socket usage

# Configuration

## Minimum configuration

The minimal configuration must include a host and/or port name. Each additional setting must be separated by semi-colon.

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
| DefaultOperationTimeout | Sets the default timeout for Redis commands                                                                                                       | Timespan                       | Optional                                                        |
| ConnectTimeout          | Sets the timeout for connecting to a single Redis node (includes auth and initialising commands)                                                  | Timespan                       | Optional                                                        |
| HealthCheckInterval	  | Sets the interval between health checks from the client to the server                                                                             | Timespan                       | Optional                                                        |
| ConnectionPoolSize      | Sets a number which creates a pool of available connections                                                                                       | Integer (> 0)                  | Optional (will default to 1)                                    |
| ReadBufferSize          | Sets the size of the network read buffer                                                                                                          | Integer                        | Optional                                                        |
| WriteBufferSize         | Sets the size of the network write buffer                                                                                                         | Integer                        | Optional                                                        |
| PipelineMode            | Sets the mode of retrieval from the TCP connection (AsyncPipeline will pipeline multiple commands, Sync will send one command at a time)          | AsyncPipeline|Sync             | Optional (defaults to AsyncPipeline)                            |
| PortMappings            | Used to apply a set of port mappings from an external port to an internal Redis port (e.g. when using a container environment with port mappings) | CSV (e.g. from1:to1,from2:to2) | Optional                                                        |

# Features

* Support for SSL
* Flexible thread management
* Support for clusters and replica configurations
* Support for master/slave configurations

# Benchmarks

[See benchmarks here](docs/benchmarks/RedisSlimClient.Benchmarks.RedisClientBenchmarks-report-github.md)

# TODO

* Support for clustering redirection
* Keep alive & socket monitoring
* Better memory management
* Slimmer object serialization
* Retry and TRYAGAIN response handling
* Support for GEO commands
