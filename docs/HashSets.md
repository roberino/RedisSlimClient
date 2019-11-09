# GetHashSetAsync and Persistent Dictionaries

The GetHashSetAsync method allows you to load and manage values though a thread safe IDictionary<string, T> implementation.

The dictionary has extended capabilities allowing the values to be saved again back to the remote database.

Data can also be reconciled with any updated values saved remotely in the database.

## Basic usage

```cs
	var dictionary = await client.GetHashSetAsync<MyObject>("my-set-1");

	dictionary["x"] = new MyObject { Value = 1 };
	dictionary["y"] = new MyObject { Value = 2 };

	await dictionary.SaveAsync();

```