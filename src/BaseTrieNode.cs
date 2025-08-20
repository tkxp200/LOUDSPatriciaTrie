namespace LOUDSTrieUtil;

public class BaseTrieNode
{
    public Dictionary<byte, BaseTrieNode> childs { get; } = new();
    public byte? key { get; }
    public bool leaf { get; }
    public int index { get; }

    public BaseTrieNode(byte? key, bool leaf, int index)
    {
        this.key = key;
        this.leaf = leaf;
        this.index = index;
    }

    public void AddChild(byte b, BaseTrieNode child)
    {
        childs.Add(b, child);
    }
}
