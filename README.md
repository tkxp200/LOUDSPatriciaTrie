# LOUDS Patricia Trie

This repository provides a memory-efficient, static Patricia Trie implemented in C# using LOUDS (Level-Order Unary Degree Sequence).

## Usage

### Exact Match Search

- Example

```cs
using LOUDSPatriciaTrie;

Dictionary<string, List<int>> list = new(){
	["an"]  = new List<int>{10},
	["i"]   = new List<int>{20, 30},
	["of"]  = new List<int>{40},
	["off"] = new List<int>{50},
	["one"] = new List<int>{50},
	["our"] = new List<int>{60},
	["out"] = new List<int>{70},
};

var trie = new LOUDSTrie<int>(list);

int[] result = trie.ExactMatchSearch("i");
Console.WriteLine($"search 'i': {string.Join(", ", result)}");
result = trie.ExactMatchSearch("ou");
Console.WriteLine($"search 'ou': {string.Join(", ", result)}");
```

- Result

```
search 'i': 20, 30
search 'ou':
```

### Common Prefix Search

- Example

```cs
using LOUDSPatriciaTrie;

Dictionary<string, List<int>> list = new(){
	["an"]  = new List<int>{10},
	["i"]   = new List<int>{20, 30},
	["of"]  = new List<int>{40},
	["off"] = new List<int>{50},
	["one"] = new List<int>{60},
	["our"] = new List<int>{70},
	["out"] = new List<int>{80},
};

var trie = new LOUDSTrie<int>(list);


List<(string, int[])> results = trie.CommonPrefixSearch("offer");
Console.WriteLine("common prefix search: 'offer':");
foreach(var result in results)
{
	var values = string.Join(", ", result.Item2);
	Console.WriteLine($"{result.Item1}: {values}");
}
```

- Result

```
common prefix search: 'offer':
of: 40
off: 50
```

### Predictive Search

- Example

```cs
using LOUDSPatriciaTrie;

Dictionary<string, List<int>> list = new(){
	["an"]  = new List<int>{10},
	["i"]   = new List<int>{20, 30},
	["of"]  = new List<int>{40},
	["off"] = new List<int>{50},
	["one"] = new List<int>{60},
	["our"] = new List<int>{70},
	["out"] = new List<int>{80},
};

var trie = new LOUDSTrie<int>(list);


List<(string, int[])> results = trie.PredictiveSearch("ou");
Console.WriteLine("predictive search: 'ou':");
foreach(var result in results)
{
	var values = string.Join(", ", result.Item2);
	Console.WriteLine($"{result.Item1}: {values}");
}
```

- Result

```
predictive search: 'ou':
our: 70
out: 80
```

### Save

- Example

```cs
using LOUDSPatriciaTrie;

public class SaveLOUDSTrie
{
	static Dictionary<string, List<int>> list = new(){
		["an"]  = new List<int>{10},
		["i"]   = new List<int>{20, 30},
		["of"]  = new List<int>{40},
		["off"] = new List<int>{50},
		["one"] = new List<int>{60},
		["our"] = new List<int>{70},
		["out"] = new List<int>{80},
	};

	static string filePath = "dictionary.trie";

	public static async Task Main()
	{
		var trie = new LOUDSTrie<int>(list);
		await LOUDSTrieIO.SaveTrieAsync(trie, filePath);
	}
}
```

### Load

- Example

```cs
using LOUDSPatriciaTrie;

public class LoadLOUDSTrie
{
	static string filePath = "dictionary.trie";

	public static async Task Main()
	{
		var trie = await LOUDSTrieIO.LoadTrieAsync<int>(filePath);

		List<(string, int[])> results = trie.CommonPrefixSearch("offer");
		Console.WriteLine("common prefix search: 'offer':");
		foreach(var result in results)
		{
			var values = string.Join(", ", result.Item2);
			Console.WriteLine($"{result.Item1}: {values}");
		}
	}
}
```

- Result

```
common prefix search: 'offer':
of: 40
off: 50
```

## TODO

- Code optimization

## Thanks

- [Roslynator](https://github.com/dotnet/roslynator)
- [MemoryPack](https://github.com/Cysharp/MemoryPack)
- https://takeda25.hatenablog.jp/entry/20120421/1335019644