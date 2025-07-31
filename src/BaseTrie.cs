using TrieUtil;

namespace Trie;

public class BaseTrie<T>
{
    BaseTrieNode rootNode;
    T[][] keysets = null!;

    public BaseTrie(Dictionary<string, List<T>> keysets)
    {
        rootNode = new BaseTrieNode(null, false, 0);
        Build(keysets);
    }

    public T[][] Keysets()
    {
        return keysets;
    }

    public BaseTrieNode GetRootNode()
    {
        return rootNode;
    }

    private void Build(Dictionary<string, List<T>> keysets)
    {
        this.keysets = new T[keysets.Count][];
        int count = 0;
        foreach (var keyset in keysets.OrderBy(e => e.Key))
        {
            this.keysets[count] = keyset.Value.ToArray();
            BaseTrieNode prev = rootNode;
            BaseTrieNode current;
            for (int i = 0; i < keyset.Key.Length; i++)
            {
                char c = keyset.Key[i];
                if (prev.childs.TryGetValue(c, out var child))
                {
                    prev = child;
                }
                else
                {
                    current = new BaseTrieNode(c, i == keyset.Key.Length - 1, i == keyset.Key.Length - 1 ? count : null);
                    prev.AddChild(c, current);
                    prev = current;
                }
            }
            count++;
            Console.WriteLine();
        }
    }


    public T[] Search(string key)
    {
        BaseTrieNode current = rootNode;
        foreach (var c in key)
        {
            if (current.childs.TryGetValue(c, out var next))
            {
                current = next;
            }
            else return [];
        }

        if (current.leaf && current.index != null) return keysets[(int)current.index];

        return [];
    }
}
