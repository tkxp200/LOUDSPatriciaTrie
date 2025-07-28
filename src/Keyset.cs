using System.Collections;

namespace Trie;

public class Keyset<T>
{
    public string key;
    public T value;

    public Keyset(string key, T value)
    {
        this.key = key;
        this.value = value;
    }

    public override string ToString()
    {
        return $"key: \"{key}\", value: {value}";
    }
}