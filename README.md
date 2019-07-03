[![Build status](https://ci.appveyor.com/api/projects/status/0eagkgc04t1jvg1m?svg=true)](https://ci.appveyor.com/project/roberino/redisslimclient)

[![Build Status](https://travis-ci.org/roberino/RedisSlimClient.svg?branch=master)](https://travis-ci.org/roberino/RedisSlimClient)

# RedisSlimClient

A work in progress. RedisSlimClient is a dotnet standard client for Redis, written from the ground up.

The main aims of the client are:

* To support basic Redis operations
* To enable fast POCO to Redis mapping
* To be performant with granular control over thread and socket usage

TODO:

* Support for SSL
* Keep alive & socket monitoring
* Better memory management
* Slimmer object serialization
* Flexible thread management
* Support for master/slave
* Support for clustering