using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using TrieUtil;

namespace Trie;

public class LOUDSTrie<T>
{
    private BitVector bitVector = null!;
    private readonly T[][] keysets;
    // private string[] keys = null!;
    private ReadOnlyMemory<char>[] keys = null!;
    private int?[] indexes = null!;

    public LOUDSTrie(Dictionary<string, List<T>> keysets)
    {
        var baseTrie = new BaseTrie<T>(keysets);
        this.keysets = baseTrie.Keysets().ToArray();
        Build(baseTrie);
    }

    public T[] ExactMatchSearch(string query)
    {
        var querySpan = query.AsSpan(0); // string to span<>
        int queryIndex = 0;
        int keyIndex;
        ReadOnlySpan<char> nodeKey;
        int LBSIndex = bitVector.Select1(1); // root's first child LBSIndex: Select1(1) = 2
        while (!bitVector.GetBit(LBSIndex) && querySpan.Length > 0)
        {
            keyIndex = bitVector.Rank0(LBSIndex + 1);
            nodeKey = keys[keyIndex].Span;
            if (querySpan.StartsWith(nodeKey, StringComparison.Ordinal))
            {
                queryIndex += nodeKey.Length;
                if (queryIndex == query.Length)
                {
                    var index = indexes[keyIndex];
                    if (index != null) return keysets[(int)index];
                    else return [];
                }
                querySpan = query.AsSpan(queryIndex);
                LBSIndex = bitVector.Select1(keyIndex);
            }
            else
            {
                LBSIndex++;
            }
        }
        return [];
    }

    private (int, string, T[]) PrefixSearch(string query)
    {
        var querySpan = query.AsSpan(0);
        int queryIndex = 0;
        int keyIndex;
        ReadOnlySpan<char> nodeKey;
        int LBSIndex = bitVector.Select1(1);
        var builder = new DefaultInterpolatedStringHandler();
        while (!bitVector.GetBit(LBSIndex) && querySpan.Length > 0)
        {
            keyIndex = bitVector.Rank0(LBSIndex + 1);
            nodeKey = keys[keyIndex].Span;
            if (querySpan.StartsWith(nodeKey, StringComparison.Ordinal))
            {
                queryIndex += nodeKey.Length;
                builder.AppendFormatted(nodeKey);
                if (queryIndex == query.Length)
                {
                    var index = indexes[keyIndex];
                    if (index != null) return (LBSIndex, query, keysets[(int)index]);
                    else return (LBSIndex, query, []);
                }
                querySpan = query.AsSpan(queryIndex);
                LBSIndex = bitVector.Select1(keyIndex);
            }
            else if (nodeKey.StartsWith(querySpan, StringComparison.Ordinal))
            {
                builder.AppendFormatted(nodeKey);
                var index = indexes[keyIndex];
                if (index != null) return (LBSIndex, builder.ToStringAndClear(), keysets[(int)index]);
                else return (LBSIndex, builder.ToStringAndClear(), []);
            }
            else
            {
                LBSIndex++;
            }
        }
        return (-1, "", []);
    }

    public List<(string, T[])> PredictiveSearch(string query)
    {
        List<(string, T[])> results = new();
        var querySearch = PrefixSearch(query);
        if (querySearch.Item1 < 0) return [];
        if (querySearch.Item3.Length != 0) results.Add((querySearch.Item2, querySearch.Item3));
        var keyBuilder = new StringBuilder(querySearch.Item2);
        SearchChild(bitVector.Select1(bitVector.Rank0(querySearch.Item1 + 1)), results, keyBuilder);
        return results;
    }

    public void SearchChild(int LBSIndex, List<(string, T[])> results, StringBuilder keyBuilder)
    {
        while (!bitVector.GetBit(LBSIndex))
        {
            int keyIndex = bitVector.Rank0(LBSIndex + 1);
            var nodeKey = keys[keyIndex];
            keyBuilder.Append(nodeKey);
            var index = indexes[keyIndex];
            if (index != null) results.Add((keyBuilder.ToString(), keysets[(int)index]));
            SearchChild(bitVector.Select1(keyIndex), results, keyBuilder);
            keyBuilder.Remove(keyBuilder.Length - nodeKey.Length, nodeKey.Length);
            LBSIndex++;
        }
    }

    public List<(string, T[])> CommonPrefixSearch(string query)
    {
        List<(string, T[])> results = new();
        var keyBuilder = new DefaultInterpolatedStringHandler(0, 0);
        var querySpan = query.AsSpan(0);
        int queryIndex = 0;
        int keyIndex;
        ReadOnlySpan<char> nodeKey;
        int LBSIndex = bitVector.Select1(1);
        while (!bitVector.GetBit(LBSIndex) && querySpan.Length > 0)
        {
            keyIndex = bitVector.Rank0(LBSIndex + 1);
            nodeKey = keys[keyIndex].Span;
            if (querySpan.StartsWith(nodeKey, StringComparison.Ordinal))
            {
                keyBuilder.AppendFormatted(nodeKey);
                queryIndex += nodeKey.Length;
                var index = indexes[keyIndex];
                if (index != null) results.Add((keyBuilder.ToString(), keysets[(int)index]));
                querySpan = query.AsSpan(queryIndex);
                LBSIndex = bitVector.Select1(keyIndex);
            }
            else
            {
                LBSIndex++;
            }
        }
        keyBuilder.ToStringAndClear();
        return results;
    }

    public string Debug()
    {
        var builder = new StringBuilder();
        builder.Append(bitVector.ToString());
        builder.AppendLine("keysets:");
        foreach (var keyset in keysets)
        {
            builder.AppendLine(keyset.ToString());
        }
        builder.AppendLine();
        builder.AppendLine("keys:");
        foreach (var key in keys)
        {
            builder.AppendLine(CultureInfo.InvariantCulture, $"\"{key}\"");
        }
        builder.AppendLine();
        builder.AppendLine("indexes:");
        foreach (var i in indexes)
        {
            builder.AppendLine(i?.ToString(CultureInfo.InvariantCulture) ?? "null");
        }
        return builder.ToString();
    }

    private void Build(BaseTrie<T> baseTrie)
    {
        var bitVectorBuilder = new BitVectorBuilder();
        List<ReadOnlyMemory<char>> keyList = new() { string.Empty.AsMemory(), string.Empty.AsMemory()};
        List<int?> indexList = new() { null, null};
        var keyBuilder = new DefaultInterpolatedStringHandler(0, 0);

        Queue<BaseTrieNode> queue = new();
        queue.Enqueue(baseTrie.GetRootNode());
        while (queue.TryDequeue(out var item))
        {
            if (item != null)
            {
                foreach (var child in item.childs)
                {
                    var current = child.Value;
                    keyBuilder.AppendFormatted(current.key);
                    while (current.childs.Count == 1 && !current.leaf)
                    {
                        current = current.childs.First().Value;
                        keyBuilder.AppendFormatted(current.key);
                    }
                    bitVectorBuilder.Add(false);
                    keyList.Add(keyBuilder.ToStringAndClear().AsMemory());
                    indexList.Add(current.index);
                    queue.Enqueue(current);
                }
                bitVectorBuilder.Add(true);
            }
        }
        bitVector = bitVectorBuilder.Build();
        keys = keyList.ToArray();
        indexes = indexList.ToArray();
    }
}
