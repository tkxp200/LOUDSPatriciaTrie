using System.Collections;
using System.Text;

namespace TrieUtil;

public class BitVector
{
    // max n = 2^32
    // lg(n)^2 = 1024 : ushort
    // lg(n)/2 = 16 : byte

    // const int BIG_BLOCK_SPLIT_SIZE = 8;
    // const int SMALL_BLOCK_SPLIT_SIZE = 4;
    const int BIG_BLOCK_SPLIT_SIZE = 1024;
    const int SMALL_BLOCK_SPLIT_SIZE = 16;

    const int SMALL_BLOCK_SIZE = (BIG_BLOCK_SPLIT_SIZE / SMALL_BLOCK_SPLIT_SIZE) + 1;

    const int MAX_OUTPUT_BITSIZE = 100;

    private BitArray _bitArray;
    int _size;
    private ushort[] _bigBlock;
    private byte[,] _smallBlock;


    public BitVector(BitArray bitArray)
    {
        _bitArray = bitArray;
        _size = _bitArray.Count;
        RoundUpBitVectorSize();
        int bigBlockSize = ((_size + BIG_BLOCK_SPLIT_SIZE - 1) / BIG_BLOCK_SPLIT_SIZE) + 1;
        _bigBlock = new ushort[bigBlockSize];
        _smallBlock = new byte[bigBlockSize - 1, SMALL_BLOCK_SIZE];

        Build();
    }

    public int Rank1(int position)
    {
        if (position > _bitArray.Count || position < 0) return -1;

        int count = 0;
        int bigIndex = position / BIG_BLOCK_SPLIT_SIZE;
        count += _bigBlock[bigIndex];
        int smallIndex = position % BIG_BLOCK_SPLIT_SIZE / SMALL_BLOCK_SPLIT_SIZE;
        count += _smallBlock[bigIndex, smallIndex];
        int start = bigIndex * BIG_BLOCK_SPLIT_SIZE + smallIndex * SMALL_BLOCK_SPLIT_SIZE;
        for (int i = start; i < position; i++)
        {
            if (_bitArray[i]) count++;
        }

        return count;
    }

    public int Select1(int count)
    {
        if (count <= 0 || count > _bitArray.Count) return -1;

        int position = 0;
        int remain = count;

        int left = -1;
        int right = _bigBlock.Length;
        while (right - left > 1)
        {
            int mid = (left + right) / 2;

            if (_bigBlock[mid] < remain) left = mid;
            else right = mid;
        }
        int bigIndex = left;
        position += bigIndex * BIG_BLOCK_SPLIT_SIZE;
        remain -= _bigBlock[bigIndex];

        left = -1;
        right = _smallBlock.GetLength(1);
        while (right - left > 1)
        {
            int mid = (left + right) / 2;

            if (_smallBlock[bigIndex, mid] < remain) left = mid;
            else right = mid;
        }
        int smallIndex = left;
        position += smallIndex * SMALL_BLOCK_SPLIT_SIZE;
        remain -= _smallBlock[bigIndex, smallIndex];

        while (remain > 0)
        {
            if (_bitArray[position]) remain--;
            position++;
        }
        return position;
    }

    public int Select0(int count)
    {
        if (count <= 0 || count > _bitArray.Count) return -1;

        int position = 0;
        int remain = count;

        int left = -1;
        int right = _bigBlock.Length;
        while (right - left > 1)
        {
            int mid = (left + right) / 2;
            int index = mid * BIG_BLOCK_SPLIT_SIZE;
            if (index - _bigBlock[mid] < remain) left = mid;
            else right = mid;
        }
        int bigIndex = left;
        position += bigIndex * BIG_BLOCK_SPLIT_SIZE;
        remain -= position - _bigBlock[bigIndex];

        left = -1;
        right = _smallBlock.GetLength(1);
        while (right - left > 1)
        {
            int mid = (left + right) / 2;
            int index = mid * SMALL_BLOCK_SPLIT_SIZE;
            if (index - _smallBlock[bigIndex, mid] < remain) left = mid;
            else right = mid;
        }
        int smallIndex = left;
        position += smallIndex * SMALL_BLOCK_SPLIT_SIZE;
        remain -= smallIndex * SMALL_BLOCK_SPLIT_SIZE - _smallBlock[bigIndex, smallIndex];

        while (remain > 0)
        {
            if (!_bitArray[position]) remain--;
            position++;
        }
        return position;
    }

    public bool Get(int index)
    {
        return _bitArray[index];
    }

    private void RoundUpBitVectorSize()
    {
        for (int i = 0; i < (BIG_BLOCK_SPLIT_SIZE - _size % BIG_BLOCK_SPLIT_SIZE) % BIG_BLOCK_SPLIT_SIZE; i++)
        {
            _bitArray.Length++;
            _bitArray[_size + i] = false;
        }
    }

    private void Build()
    {
        byte smallCount = 0;
        ushort bigCount = 0;
        int bigBlockIndex = 1;
        int smallBlockIndex = 1;
        // Console.WriteLine($"BIG_BLOCK_SPLIT_SIZE: {BIG_BLOCK_SPLIT_SIZE}");
        // Console.WriteLine($"SMALL_BLOCK_SPLIT_SIZE: {SMALL_BLOCK_SPLIT_SIZE}");
        for (int i = 0; i < _bitArray.Count; i++)
        {
            // Console.WriteLine($"i: {i}");
            // if(i < _size) Console.WriteLine($"bit: {_bitArray[i]}");
            if (i < _size && _bitArray[i])
            {
                smallCount++;
                bigCount++;
            }
            // Console.WriteLine($"i + 1 % SMALL_BLOCK_SPLIT_SIZE == 0: {(i + 1) % SMALL_BLOCK_SPLIT_SIZE == 0}");
            if ((i + 1) % SMALL_BLOCK_SPLIT_SIZE == 0)
            {
                _smallBlock[bigBlockIndex - 1, smallBlockIndex] = (byte)(smallCount + _smallBlock[bigBlockIndex - 1, smallBlockIndex - 1]);
                smallCount = 0;
                smallBlockIndex++;
            }
            // Console.WriteLine($"i + 1 % BIG_BLOCK_SPLIT_SIZE == 0: {(i + 1) % BIG_BLOCK_SPLIT_SIZE == 0}");
            if ((i + 1) % BIG_BLOCK_SPLIT_SIZE == 0)
            {
                _bigBlock[bigBlockIndex] = (ushort)(bigCount + _bigBlock[bigBlockIndex - 1]);
                bigCount = 0;
                bigBlockIndex++;
                smallBlockIndex = 1;
            }
        }
    }

    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.AppendLine($"BitArraySize: {_bitArray.Count}");
        builder.AppendLine($"BigBlockSize: {_bigBlock.Length}");
        builder.AppendLine($"SmallBlockSize: {_smallBlock.GetLength(0)},{_smallBlock.GetLength(1)}");
        builder.AppendLine();

        if (_size > MAX_OUTPUT_BITSIZE) return builder.ToString();

        builder.AppendLine("BitArray:");
        for (int i = 0; i < _bitArray.Count; i++)
        {
            builder.Append(_bitArray[i] ? 1 : 0);
        }
        builder.AppendLine();
        builder.AppendLine();

        builder.AppendLine("BigBlock:");
        builder.AppendLine(string.Join(",", _bigBlock));
        builder.AppendLine();

        builder.AppendLine("smallBlock:");
        for (int i = 0; i < _smallBlock.GetLength(0); i++)
        {
            var rowArray = Enumerable.Range(0, _smallBlock.GetLength(1))
                                    .Select(j => _smallBlock[i, j]);
            builder.AppendLine(string.Join(",", rowArray));
        }
        builder.AppendLine();

        return builder.ToString();
    }
}

public class BitVectorBuilder
{
    const int MAX_OUTPUT_BITSIZE = 100;
    private BitArray _bitArray;

    public BitVectorBuilder()
    {
        _bitArray = new BitArray(2);

        AddRoot();
    }

    private void AddRoot()
    {
        _bitArray[0] = true;
        _bitArray[1] = false;
    }

    public void Add(bool value)
    {
        _bitArray.Length++;
        int bitCount = _bitArray.Count;
        _bitArray[bitCount - 1] = value;
    }

    public BitVector Build()
    {
        var bitVector = new BitVector(_bitArray);
        return bitVector;
    }

    public override string ToString()
    {
        if (_bitArray.Count > MAX_OUTPUT_BITSIZE)
            return "BitArrayのサイズが大きすぎます. BitVectorBuilder.ToString()はデバッグ用メソッドです. 小さいデータで行ってください。";

        var builder = new StringBuilder();

        builder.AppendLine("BitVector:");
        for (int i = 0; i < _bitArray.Count; i++)
        {
            builder.Append(_bitArray[i] ? 1 : 0);
        }

        return builder.ToString();
    }

}
