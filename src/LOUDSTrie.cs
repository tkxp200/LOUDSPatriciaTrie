using System.Text;
using TrieUtil;

namespace Trie;

public class LOUDSTrie<T>
{
    BitVector bitVector = null!;
    T[][] keysets;
    string[] keys = null!;
    int?[] indexes = null!;

    public LOUDSTrie(Dictionary<string, List<T>> keysets)
    {
        var baseTrie = new BaseTrie<T>(keysets);
        this.keysets = baseTrie.Keysets();
        Build(baseTrie);
    }

    public T[] Search(string query)
    {
        var querySpan = query.AsSpan(0); // string to span<>
        int queryIndex = 0;
        int keyIndex;
        string nodeKey;
        int LBSIndex = bitVector.Select0(1); // root's first child LBSIndex: Select0(1) = 2
        while (bitVector.Get(LBSIndex) && querySpan.Length > 0)
        {
            keyIndex = bitVector.Rank1(LBSIndex + 1);
            nodeKey = keys[keyIndex];
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
                LBSIndex = bitVector.Select0(keyIndex);
            }
            else
            {
                LBSIndex++;
            }
        }
        return [];
    }

    public List<(string, T[])> CommonPrefixSearch(string query)
    {
        List<(string, T[])> results = new();
        var keyBuilder = new StringBuilder();
        var querySpan = query.AsSpan(0);
        int queryIndex = 0;
        int keyIndex;
        string nodeKey;
        int LBSIndex = bitVector.Select0(1);
        while (bitVector.Get(LBSIndex) && querySpan.Length > 0)
        {
            keyIndex = bitVector.Rank1(LBSIndex + 1);
            nodeKey = keys[keyIndex];
            if (querySpan.StartsWith(nodeKey, StringComparison.Ordinal))
            {
                keyBuilder.Append(nodeKey);
                queryIndex += nodeKey.Length;
                var index = indexes[keyIndex];
                if (index != null) results.Add((keyBuilder.ToString(), keysets[(int)index]));
                querySpan = query.AsSpan(queryIndex);
                LBSIndex = bitVector.Select0(keyIndex);
            }
            else
            {
                LBSIndex++;
            }
        }
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
            builder.AppendLine($"\"{key}\"");
        }
        builder.AppendLine();
        builder.AppendLine("indexes:");
        foreach (var i in indexes)
        {
            builder.AppendLine(i?.ToString() ?? "null");
        }
        return builder.ToString();
    }

    private void Build(BaseTrie<T> baseTrie)
    {
        var bitVectorBuilder = new BitVectorBuilder();
        List<string> keyList = new() { "", ""};
        List<int?> indexList = new() { null, null};

        Queue<BaseTrieNode> queue = new();
        queue.Enqueue(baseTrie.GetRootNode());
        while (queue.TryDequeue(out var item))
        {
            if (item != null)
            {
                foreach (var child in item.childs)
                {
                    var current = child.Value;
                    var keyBuilder = new StringBuilder(current.key.ToString());
                    while (current.childs.Count == 1 && !current.leaf)
                    {
                        current = current.childs.First().Value;
                        keyBuilder.Append(current.key);
                    }
                    bitVectorBuilder.Add(true);
                    keyList.Add(keyBuilder.ToString());
                    indexList.Add(current.index);
                    queue.Enqueue(current);
                }
                bitVectorBuilder.Add(false);
            }
        }
        bitVector = bitVectorBuilder.Build();
        keys = keyList.ToArray();
        indexes = indexList.ToArray();
    }
}
