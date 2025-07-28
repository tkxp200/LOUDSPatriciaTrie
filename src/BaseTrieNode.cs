namespace TrieUtil;

public class BaseTrieNode
{
    public Dictionary<char, BaseTrieNode> childs = new();
    public char? key;
    public bool leaf;
    public int? index;

    public BaseTrieNode(char? key, bool leaf, int? index)
    {
        this.key = key;
        this.leaf = leaf;
        this.index = index;
    }

    public void AddChild(char c, BaseTrieNode child)
    {
        childs.Add(c, child);
    }
}
