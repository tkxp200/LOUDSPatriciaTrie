using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using LOUDSTrieUtil;
using MemoryPack;

namespace LOUDSPatriciaTrie;

[MemoryPackable]
public partial class LOUDSTrie<T>
{
    [MemoryPackOrder(0)]
    public BitVector bitVector { get; private set; } = null!;
    [MemoryPackOrder(1)]
    public T[][] keysets { get; }
    // private string[] keys = null!;
    // public ReadOnlyMemory<char>[] keys { get; private set; } = null!;
    [MemoryPackOrder(2)]
    public char[] tailKeys { get; private set; } = null!;
    [MemoryPackOrder(3)]
    public BitVector tailBits { get; private set; } = null!;
    [MemoryPackOrder(4)]
    public int[] indexes { get; private set; } = null!;

    public LOUDSTrie(Dictionary<string, List<T>> keysets)
    {
        var baseTrie = new BaseTrie<T>(keysets);
        this.keysets = baseTrie.Keysets().ToArray();
        Build(baseTrie);
    }

    [MemoryPackConstructor]
    public LOUDSTrie(BitVector bitVector, T[][] keysets, char[] tailKeys, BitVector tailBits, int[] indexes)
    {
        this.bitVector = bitVector;
        this.keysets = keysets;
        this.tailKeys = tailKeys;
        this.tailBits = tailBits;
        this.indexes = indexes;
    }

    public T[] ExactMatchSearch(string query)
    {
        return ExactMatchSearch(query.AsSpan());
    }

    public T[] ExactMatchSearch(ReadOnlySpan<char> query)
    {
        var span = query;
        int nodeNumber;
        int tailStartIndex;
        int tailEndIndex;
        ReadOnlySpan<char> nodeKey;
        int LBSIndex = bitVector.Select1(1); // root's first child LBSIndex: Select1(1) = 2
        while (!bitVector.GetBit(LBSIndex) && span.Length > 0)
        {
            nodeNumber = bitVector.Rank0(LBSIndex + 1);
            tailStartIndex = tailBits.Select1(nodeNumber) - 1;
            tailEndIndex = tailBits.Select1(nodeNumber + 1) - 1;
            if (tailEndIndex >= 0)
                nodeKey = tailKeys[tailStartIndex..tailEndIndex].AsSpan();
            else
                nodeKey = tailKeys[tailStartIndex..].AsSpan();
            if (span.StartsWith(nodeKey, StringComparison.Ordinal))
            {
                if (span.Length == nodeKey.Length)
                {
                    var index = indexes[nodeNumber];
                    if (index >= 0) return keysets[index];
                    else return [];
                }
                span = span.Slice(nodeKey.Length);
                LBSIndex = bitVector.Select1(nodeNumber);
            }
            else
            {
                LBSIndex++;
            }
        }
        return [];
    }

    private (int, string, T[]) PrefixSearch(ReadOnlySpan<char> query)
    {
        var span = query;
        int nodeNumber;
        int tailStartIndex;
        int tailEndIndex;
        ReadOnlySpan<char> nodeKey;
        int LBSIndex = bitVector.Select1(1);
        var builder = new StringBuilder();
        while (!bitVector.GetBit(LBSIndex) && span.Length > 0)
        {
            nodeNumber = bitVector.Rank0(LBSIndex + 1);
            tailStartIndex = tailBits.Select1(nodeNumber) - 1;
            tailEndIndex = tailBits.Select1(nodeNumber + 1) - 1;
            if (tailEndIndex >= 0)
                nodeKey = tailKeys[tailStartIndex..tailEndIndex].AsSpan();
            else
                nodeKey = tailKeys[tailStartIndex..].AsSpan();
            if (span.StartsWith(nodeKey, StringComparison.Ordinal))
            {
                builder.Append(nodeKey);
                if (span.Length == nodeKey.Length)
                {
                    var index = indexes[nodeNumber];
                    if (index >= 0) return (LBSIndex, query.ToString(), keysets[index]);
                    else return (LBSIndex, query.ToString(), []);
                }
                span = span.Slice(nodeKey.Length);
                LBSIndex = bitVector.Select1(nodeNumber);
            }
            else if (nodeKey.StartsWith(span, StringComparison.Ordinal))
            {
                builder.Append(nodeKey);
                var index = indexes[nodeNumber];
                if (index >= 0) return (LBSIndex, builder.ToString(), keysets[index]);
                else return (LBSIndex, builder.ToString(), []);
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
        return PredictiveSearch(query.AsSpan());
    }

    public List<(string, T[])> PredictiveSearch(ReadOnlySpan<char> query)
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
            int nodeNumber = bitVector.Rank0(LBSIndex + 1);
            ReadOnlySpan<char> nodeKey;
            int tailStartIndex = tailBits.Select1(nodeNumber) - 1;
            int tailEndIndex = tailBits.Select1(nodeNumber + 1) - 1;
            if (tailEndIndex >= 0)
                nodeKey = tailKeys[tailStartIndex..tailEndIndex].AsSpan();
            else
                nodeKey = tailKeys[tailStartIndex..].AsSpan();
            keyBuilder.Append(nodeKey);
            var index = indexes[nodeNumber];
            if (index >= 0) results.Add((keyBuilder.ToString(), keysets[index]));
            SearchChild(bitVector.Select1(nodeNumber), results, keyBuilder);
            keyBuilder.Remove(keyBuilder.Length - nodeKey.Length, nodeKey.Length);
            LBSIndex++;
        }
    }

    public List<(string, T[])> CommonPrefixSearch(string query)
    {
        return CommonPrefixSearch(query.AsSpan());
    }

    public List<(string, T[])> CommonPrefixSearch(ReadOnlySpan<char> query)
    {
        List<(string, T[])> results = new();
        var keyBuilder = new StringBuilder();
        var span = query;
        int nodeNumber;
        int tailStartIndex;
        int tailEndIndex;
        ReadOnlySpan<char> nodeKey;
        int LBSIndex = bitVector.Select1(1);
        while (!bitVector.GetBit(LBSIndex) && span.Length > 0)
        {
            nodeNumber = bitVector.Rank0(LBSIndex + 1);
            tailStartIndex = tailBits.Select1(nodeNumber) - 1;
            tailEndIndex = tailBits.Select1(nodeNumber + 1) - 1;
            if (tailEndIndex >= 0)
                nodeKey = tailKeys[tailStartIndex..tailEndIndex].AsSpan();
            else
                nodeKey = tailKeys[tailStartIndex..].AsSpan();
            if (span.StartsWith(nodeKey, StringComparison.Ordinal))
            {
                keyBuilder.Append(nodeKey);
                var index = indexes[nodeNumber];
                if (index >= 0) results.Add((keyBuilder.ToString(), keysets[index]));
                span = span.Slice(nodeKey.Length);
                LBSIndex = bitVector.Select1(nodeNumber);
            }
            else
            {
                LBSIndex++;
            }
        }
        return results;
    }

    private string Debug()
    {
        var builder = new StringBuilder();
        builder.Append(bitVector.Debug());
        builder.AppendLine("keysets:");
        foreach (var keyset in keysets)
        {
            builder.AppendLine(keyset.ToString());
        }
        builder.AppendLine();
        builder.Append("tailKeys:");
        builder.Append(tailKeys);
        builder.AppendLine();
        builder.Append(tailBits.Debug());
        builder.AppendLine();
        builder.AppendLine("indexes:");
        foreach (var i in indexes)
        {
            builder.AppendLine(i.ToString(CultureInfo.InvariantCulture));
        }
        return builder.ToString();
    }

    private void Build(BaseTrie<T> baseTrie)
    {
        var bitVectorBuilder = new BitVectorBuilder();
        StringBuilder tailBuilder = new(new string(' ', 2));
        var tailBitVector = new BitVectorBuilder();
        List<int> indexList = new() { -1, -1};

        Queue<BaseTrieNode> queue = new();
        queue.Enqueue(baseTrie.GetRootNode());
        while (queue.TryDequeue(out var item))
        {
            if (item is not null)
            {
                foreach (var child in item.childs)
                {
                    var current = child.Value;
                    tailBuilder.Append(current.key);
                    tailBitVector.Add(true);
                    while (current.childs.Count == 1 && !current.leaf)
                    {
                        current = current.childs.First().Value;
                        tailBuilder.Append(current.key);
                        tailBitVector.Add(false);
                    }
                    bitVectorBuilder.Add(false);
                    indexList.Add(current.index);
                    queue.Enqueue(current);
                }
                bitVectorBuilder.Add(true);
            }
        }
        bitVector = bitVectorBuilder.Build();
        tailBits = tailBitVector.Build();
        tailKeys = tailBuilder.ToString().ToArray();
        indexes = indexList.ToArray();
    }
}
