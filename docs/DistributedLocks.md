# GetHashSetAsync and Persistent Dictionaries

The AquireLockAsync method is an implementation of the [Redlock](https://redis.io/topics/distlock) algorithm
and can be used to create a distributed lock across systems using your Redis DB.

## Basic usage

```cs
	using (var client = config.CreateClient())
    {
        await client.PingAllAsync();

        var lockKey = "MyResourceToLockName";

        using (var asyncLock = await client.AquireLockAsync(lockKey, new LockOptions(TimeSpan.FromSeconds(5), true)))
        {
            // My locked code:

			CallMyFunctionRequiringSyncronisation();

			// Best to release asyncronously:

            await asyncLock.ReleaseLockAsync(); 
        }
    }

```