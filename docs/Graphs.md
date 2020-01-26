# Graphs

The graph client interface offers limitted support for graphs within Redis. 
Currently, the implementation uses Redis hash sets as a storage mechanism
but with a long term view of implementing support for the graph module 
offered with Redis.

The Query engine is very basic and intended to mimic Gremlin style queries
but the implementation is far from complete.

## Basic usage

```cs
var graphNamespace = "graph-123";

var graph = client.GetGraph(graphNamespace);

var x = await graph.GetVertexAsync<string>("x");
var y = await graph.GetVertexAsync<string>("y");

x.Label = "x";

var edge = x.Connect(y.Id, "eq");

await x.SaveAsync();

var results = await x.QueryAsync(Query<string>
    .Create()
    .HasLabel("x")
    .Out("eq")
    .Build());

```