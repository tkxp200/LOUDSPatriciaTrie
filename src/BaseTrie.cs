using TrieUtil;

namespace Trie;

public class BaseTrie<T>
{
    List<Keyset<T>> _keysets;
    BaseTrieNode rootNode;

    public BaseTrie(List<Keyset<T>> keysets)
    {
        _keysets = keysets.OrderBy(e => e.key).ToList();
        rootNode = new BaseTrieNode(null, false, 0);
        Build();
    }

    public BaseTrieNode GetRootNode()
    {
        return rootNode;
    }

    private void Build()
    {
        int count = 0;
        foreach (var keyset in _keysets)
        {
            BaseTrieNode prev = rootNode;
            BaseTrieNode current;
            for (int i = 0; i < keyset.key.Length; i++)
            {
                char c = keyset.key[i];
                if (prev.childs.TryGetValue(c, out var child))
                {
                    prev = child;
                }
                else
                {
                    current = new BaseTrieNode(c, i == keyset.key.Length - 1, i == keyset.key.Length - 1 ? count : null);
                    prev.AddChild(c, current);
                    prev = current;
                }
            }
            count++;
            Console.WriteLine();
        }
    }

    public Keyset<T>? Search(string key)
    {
        BaseTrieNode current = rootNode;
        foreach (var c in key)
        {
            if (current.childs.TryGetValue(c, out var next))
            {
                current = next;
            }
            else return null;
        }

        if (current.leaf && current.index != null) return _keysets[(int)current.index];

        return null;
    }
}
