# Patricia LOUDS Trie

This repository provides a memory-efficient, static Patricia Trie implemented in C# using LOUDS (Level-Order Unary Degree Sequence).

## Usage

- Example

```cs
using Trie;


Dictionary<string, List<int>> list = new(){
	["an"]  = new List<int>{10},
	["i"]   = new List<int>{20, 30},
	["of"]  = new List<int>{40},
	["one"] = new List<int>{50},
	["our"] = new List<int>{60},
	["out"] = new List<int>{70},
};

var trie = new LOUDSTrie<int>(list);

var result = trie.Search("i");
Console.WriteLine(result != null ? string.Join(", ", result) : "i: not found");
result = trie.Search("ou");
Console.WriteLine(result != null ? string.Join(", ", result) : "ou: not found");
```

- Result

```
20, 30
ou: not found
```

## TODO

- Multiple values for the same key
- CommonPrefix Search
- Predictive Search
- Save/Load

## Thanks

- https://takeda25.hatenablog.jp/entry/20120421/1335019644