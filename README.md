# Patricia LOUDS Trie

This repository provides a memory-efficient, static Patricia Trie implemented in C# using LOUDS (Level-Order Unary Degree Sequence).

## Usage

- Example

```cs
using Trie;


List<Keyset<int>> list = new();
list.Add(new Keyset<int>("an", 10));
list.Add(new Keyset<int>("i", 20));
list.Add(new Keyset<int>("of", 30));
list.Add(new Keyset<int>("one", 40));
list.Add(new Keyset<int>("our", 50));
list.Add(new Keyset<int>("out", 60));

var trie = new LOUDSTrie<int>(list);

var result = trie.Search("i");
Console.WriteLine(result != null ? result.ToString() : "i: null");

result = trie.Search("ou");
Console.WriteLine(result != null ? result.ToString() : "ou: null");
```

- Result

```
key: "i", value: 20
ou: null
```

## TODO

- CommonPrefix Search
- Predictive Search

## Thanks

- https://takeda25.hatenablog.jp/entry/20120421/1335019644