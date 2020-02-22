# Geographic Commands

The Geo API implements the GEO commands offered by Redis.

## Basic usage

```cs

var key = "my-geography"

await client.GeoAddAsync(key, new[] {
                    ("London", (-0.118092, 51.509865)),
                    ("New York", (-73.93524, 40.730610)) });

var radiusResults = await client.GeoRadiusAsync(
    new GeoRadiusQuery(key,
        centrePoint: (120, 60),
        radius: 1000,
        distanceUnit: DistanceUnit.Kilometres,
        sortOrder: Types.SortOrder.Ascending,
        options: GeoRadiusOptions.WithCoord | GeoRadiusOptions.WithHash));

foreach(var result in radiusResults)
{
    Console.WriteLine($"{result.Key}: {result.Value}");
}

```