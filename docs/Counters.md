# Counters

Distributed counters provide a simple interface around Redis increment commands. 

## Basic usage

```cs
var id = "my-counter"

var counter = await client.GetCounter(id);

var val = await counter.ReadAsync();

var newVal = await counter.IncrementAsync();

```