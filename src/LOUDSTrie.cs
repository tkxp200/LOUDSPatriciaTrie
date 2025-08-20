using System.Buffers;
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
    [MemoryPackOrder(2)]
    public byte[]?[] keys { get; private set; } = null!;
    [MemoryPackOrder(3)]
    public int[] indexes { get; private set; } = null!;

    public LOUDSTrie(Dictionary<string, List<T>> keysets)
    {
        var baseTrie = new BaseTrie<T>(keysets);
        this.keysets = baseTrie.Keysets().ToArray();
        Build(baseTrie);
    }

    [MemoryPackConstructor]
    public LOUDSTrie(BitVector bitVector, T[][] keysets, byte[][] keys, int[] indexes)
    {
        this.bitVector = bitVector;
        this.keysets = keysets;
        this.keys = keys;
        this.indexes = indexes;
    }

    public T[] ExactMatchSearch(string query)
    {
        return ExactMatchSearch(query.AsSpan());
    }

    public T[] ExactMatchSearch(ReadOnlySpan<char> query)
    {
        int maxByteCount = Encoding.UTF8.GetMaxByteCount(query.Length);
        Span<byte> buffer = new byte[maxByteCount];
        var written = Encoding.UTF8.GetBytes(query, buffer);
        var span = buffer.Slice(0, written);
        int keyIndex;
        ReadOnlySpan<byte> nodeKey;
        int LBSIndex = bitVector.Select1(1); // root's first child LBSIndex: Select1(1) = 2
        while (!bitVector.GetBit(LBSIndex) && span.Length > 0)
        {
            keyIndex = bitVector.Rank0(LBSIndex + 1);
            nodeKey = keys[keyIndex].AsSpan();
            if (span.StartsWith(nodeKey))
            {
                if (span.Length == nodeKey.Length)
                {
                    var index = indexes[keyIndex];
                    if (index >= 0) return keysets[index];
                    else return [];
                }
                span = span.Slice(nodeKey.Length);
                LBSIndex = bitVector.Select1(keyIndex);
            }
            else
            {
                LBSIndex++;
            }
        }
        return [];
    }

    private (int, MemoryStream, T[]) PrefixSearch(ReadOnlySpan<byte> query)
    {
        var span = query;
        int keyIndex;
        ReadOnlySpan<byte> nodeKey;
        int LBSIndex = bitVector.Select1(1);
        var builder = new MemoryStream();
        while (!bitVector.GetBit(LBSIndex) && span.Length > 0)
        {
            keyIndex = bitVector.Rank0(LBSIndex + 1);
            nodeKey = keys[keyIndex].AsSpan();
            if (span.StartsWith(nodeKey))
            {
                builder.Write(nodeKey);
                if (span.Length == nodeKey.Length)
                {
                    var index = indexes[keyIndex];
                    if (index >= 0)
                    {
                        builder.SetLength(0);
                        builder.Write(query);
                        return (LBSIndex, builder, keysets[index]);
                    }
                    else
                    {
                        builder.SetLength(0);
                        builder.Write(query);
                        return (LBSIndex, builder, []);
                    }
                }
                span = span.Slice(nodeKey.Length);
                LBSIndex = bitVector.Select1(keyIndex);
            }
            else if (nodeKey.StartsWith(span))
            {
                builder.Write(nodeKey);
                var index = indexes[keyIndex];
                if (index >= 0) return (LBSIndex, builder, keysets[index]);
                else return (LBSIndex, builder, []);
            }
            else
            {
                LBSIndex++;
            }
        }
        return (-1, builder, []);
    }

    public List<(string, T[])> PredictiveSearch(string query)
    {
        return PredictiveSearch(query.AsSpan());
    }

    public List<(string, T[])> PredictiveSearch(ReadOnlySpan<char> query)
    {
        int maxByteCount = Encoding.UTF8.GetMaxByteCount(query.Length);
        Span<byte> buffer = new byte[maxByteCount];
        var written = Encoding.UTF8.GetBytes(query, buffer);
        var span = buffer.Slice(0, written);

        List<(string, T[])> results = new();
        var querySearch = PrefixSearch(span);
        if (querySearch.Item1 < 0) return [];
        if (querySearch.Item3.Length != 0) results.Add((Encoding.UTF8.GetString(querySearch.Item2.GetBuffer(), 0, (int)querySearch.Item2.Length), querySearch.Item3));
        SearchChild(bitVector.Select1(bitVector.Rank0(querySearch.Item1 + 1)), results, querySearch.Item2);
        return results;
    }

    public void SearchChild(int LBSIndex, List<(string, T[])> results, MemoryStream keyBuilder)
    {
        while (!bitVector.GetBit(LBSIndex))
        {
            int keyIndex = bitVector.Rank0(LBSIndex + 1);
            var nodeKey = keys[keyIndex].AsSpan();
            keyBuilder.Write(nodeKey);
            var index = indexes[keyIndex];
            if (index >= 0) results.Add((Encoding.UTF8.GetString(keyBuilder.GetBuffer(), 0, (int)keyBuilder.Length), keysets[index]));
            SearchChild(bitVector.Select1(keyIndex), results, keyBuilder);
            keyBuilder.SetLength(keyBuilder.Length - nodeKey.Length);
            LBSIndex++;
        }
    }

    public List<(string, T[])> CommonPrefixSearch(string query)
    {
        return CommonPrefixSearch(query.AsSpan());
    }

    public List<(string, T[])> CommonPrefixSearch(ReadOnlySpan<char> query)
    {
        int maxByteCount = Encoding.UTF8.GetMaxByteCount(query.Length);
        Span<byte> buffer = new byte[maxByteCount];
        var written = Encoding.UTF8.GetBytes(query, buffer);
        var span = buffer.Slice(0, written);
        List<(string, T[])> results = new();
        var keyBuilder = new ArrayBufferWriter<byte>();
        int keyIndex;
        ReadOnlySpan<byte> nodeKey;
        int LBSIndex = bitVector.Select1(1);
        while (!bitVector.GetBit(LBSIndex) && span.Length > 0)
        {
            keyIndex = bitVector.Rank0(LBSIndex + 1);
            nodeKey = keys[keyIndex].AsSpan();
            if (span.StartsWith(nodeKey))
            {
                keyBuilder.Write(nodeKey);
                // queryIndex += nodeKey.Length;
                var index = indexes[keyIndex];
                if (index >= 0) results.Add((Encoding.UTF8.GetString(keyBuilder.WrittenSpan), keysets[index]));
                span = span.Slice(nodeKey.Length);
                LBSIndex = bitVector.Select1(keyIndex);
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
        builder.AppendLine("keys:");
        foreach (var key in keys)
        {
            if (key is not null) builder.AppendLine(CultureInfo.InvariantCulture, $"{string.Join("", key)}");
            else builder.AppendLine();
        }
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
        List<byte[]?> keyList = new() { null, null};
        List<int> indexList = new() { -1, -1};
        var keyBuilder = new MemoryStream();

        Queue<BaseTrieNode> queue = new();
        queue.Enqueue(baseTrie.GetRootNode());
        while (queue.TryDequeue(out var item))
        {
            if (item is not null)
            {
                foreach (var child in item.childs)
                {
                    var current = child.Value;
                    if (current.key is not null) keyBuilder.Write([(byte)current.key]);
                    while (current.childs.Count == 1 && !current.leaf)
                    {
                        current = current.childs.First().Value;
                        if (current.key is not null) keyBuilder.Write([(byte)current.key]);
                    }
                    bitVectorBuilder.Add(false);
                    keyList.Add(keyBuilder.ToArray());
                    keyBuilder.SetLength(0);
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
