using System.Text;

namespace LOUDSTrieUtil;

public class BaseTrie<T>
{
    private readonly BaseTrieNode rootNode;
    private List<T[]> keysets = null!;

    public BaseTrie(Dictionary<string, List<T>> keysets)
    {
        rootNode = new BaseTrieNode(null, false, 0);
        Build(keysets);
    }

    public List<T[]> Keysets()
    {
        return keysets;
    }

    public BaseTrieNode GetRootNode()
    {
        return rootNode;
    }

    private void Build(Dictionary<string, List<T>> keysets)
    {
        var entries = new List<(byte[] KeyBytes, List<T> Value)>(keysets.Count);
        foreach (var item in keysets)
        {
            entries.Add((Encoding.UTF8.GetBytes(item.Key), item.Value));
        }

        entries.Sort((a, b) => ((ReadOnlySpan<byte>)a.KeyBytes).SequenceCompareTo(b.KeyBytes));
        this.keysets = new(keysets.Count);
        int count = 0;
        foreach (var keyset in entries)
        {
            this.keysets.Add(keyset.Value.ToArray());
            BaseTrieNode prev = rootNode;
            BaseTrieNode current;
            for (int i = 0; i < keyset.KeyBytes.Length; i++)
            {
                var b = keyset.KeyBytes[i];
                if (prev.childs.TryGetValue(b, out var child))
                {
                    prev = child;
                }
                else
                {
                    current = new BaseTrieNode(b, i == keyset.KeyBytes.Length - 1, i == keyset.KeyBytes.Length - 1 ? count : -1);
                    prev.AddChild(b, current);
                    prev = current;
                }
            }
            count++;
        }
    }


    private T[] Search(string query)
    {
        var queryBytes = Encoding.UTF8.GetBytes(query);
        BaseTrieNode current = rootNode;
        foreach (var b in queryBytes)
        {
            if (current.childs.TryGetValue(b, out var next))
            {
                current = next;
            }
            else
            {
                return [];
            }
        }

        if (current.leaf && current.index >= 0) return keysets[current.index];

        return [];
    }
}
